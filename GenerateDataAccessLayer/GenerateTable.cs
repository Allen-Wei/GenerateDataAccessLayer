using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Linq;


/*
 * Example: 

   var connenction = "Data Source=(localdb)\\ProjectsV12;Initial Catalog=YSD;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False";

    GenerateTable generate = new GenerateTable(new GenerateTable.Configuration()
    {
        TemplateFolderPath = @"D:\Projects\ConsoleApp\ConsoleApp\GenerateTableTemplates",
        BaseFilePath = @"D:\Temporary",
        BaseNamespace = "ProjectName.DataAccessLayer",
        Connection = connenction,
        ModelContextName = "ModelContext",
        ModelConstructorConnection = "\"Data Source=(localdb)\\\\ProjectsV12;Initial Catalog=YSD;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False\"",
    });
    generate.SetEntities();
    generate.SaveModels();
    generate.SaveRespository();
    generate.SaveEntityModels();
    generate.SaveRepositoryModels();

 */

namespace GenerateDataAccessLayer
{

    public class GenerateTable
    {

        private Configuration Config { get; set; }
        private List<UserTable> Tables { get; set; }
        private Dictionary<string, string> Entities { get; set; }
        public GenerateTable(Configuration c)
        {
            this.Config = c;
        }

        private string ConvertType(TableColumn column)
        {

            var dbType = column.DbType.ToLower();
            switch (dbType)
            {
                case "varchar":
                case "nvarchar":
                case "ntext":
                    dbType = "string"; break;
                case "smalldatetime":
                case "datetime":
                    dbType = column.IsNullable ? "DateTime?" : "DateTime"; break;
                case "smallint":
                case "tinyint":
                    dbType = column.IsNullable ? "short?" : "short"; break;
                case "uniqueidentifier":
                    dbType = column.IsNullable ? "Guid?" : "Guid"; break;
                case "bit":
                    dbType = column.IsNullable ? "bool?" : "bool"; break;
                case "money":
                    dbType = column.IsNullable ? "decimal?" : "decimal"; break;
                case "text":
                    dbType = "string";
                    break;
                case "int":
                case "bigint":
                    dbType = column.IsNullable ? "int?" : "int";
                    break;
            }
            return dbType;
        }

        public void GetTables()
        {
            var cxt = new Context(this.Config.Connection);
            var tables = cxt.ExecuteQuery<UserTable>("select Table_Name as TableName from INFORMATION_SCHEMA.TABLES").ToList();

            tables.ForEach(t =>
            {
                t.SetPrimaryKeyNames(cxt);
                t.SetIdentityColumnNames(cxt);
                t.Columns = cxt.ExecuteQuery<TableColumn>(
                  String.Format("select COLUMN_NAME as Name, DATA_TYPE as DbType, cast(case when IS_NULLABLE = 'YES' then 1 else 0 end as bit) as IsNullable, CHARACTER_MAXIMUM_LENGTH as [Length] from INFORMATION_SCHEMA.COLUMNS where Table_Name='{0}'", t.TableName)).ToList();

                t.Columns.ForEach(c =>
                {
                    c.DataType = this.ConvertType(c);
                    c.IsPrimaryKey = t.PrimaryKeyNames.Contains(c.Name);
                    c.IsDbGenerated = t.IdentityColumnNames.Contains(c.Name);
                });
            });

            this.Tables = tables;
        }


        private string GetContext()
        {
            var contextFrame = this.Config.GetTemplate("DataContext.txt");

            var context = new StringBuilder();
            foreach (var t in this.Tables)
            {
                context.AppendFormat(@"
        public System.Data.Linq.Table<{0}> {0}
		{{
			get
			{{
				return this.GetTable<{0}>();
			}}
		}}
        ", t.TableName);
            }
            return GenerateTable.Configuration.FillPlaceholder(contextFrame, new Dictionary<string, string>() { { "Placeholder", context.ToString() } });
        }


        private void SaveToFiles(string filePath)
        {
            foreach (var row in this.Entities)
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(filePath, String.Format("{0}.cs", row.Key)), row.Value, Encoding.UTF8);
            }
        }

        /// <summary>
        /// 初始化实体类
        /// </summary>
        public void SetEntities()
        {
            this.GetTables();

            var classes = new Dictionary<string, string>();
            this.Tables.ForEach(t =>
            {
                var sb = new StringBuilder();
                sb.AppendLine(this.Config.HoverClass(t));
                sb.AppendLine(String.Format("[Table(Name = \"{0}\")]", t.TableName));
                sb.AppendLine(String.Format("public partial class {0}", t.TableName));
                sb.AppendLine("{");
                t.Columns.ForEach(c =>
                {
                    sb.AppendLine(this.Config.HoverColumn(c));
                    sb.AppendLine(String.Format("\tpublic {0} {1} {{get;set;}}", c.DataType, c.Name));
                });
                sb.AppendLine("}");

                classes.Add(t.TableName, sb.ToString());
            });
            this.Entities = classes;
        }

        public void SaveModels()
        {
            var frame = this.Config.GetTemplate("ModelFrame.txt");
            var context = this.GetContext();
            StringBuilder modelsCode = new StringBuilder(context);
            foreach (var row in this.Entities)
            {
                modelsCode.Append(row.Value);
                modelsCode.AppendLine();
            }

            var code = GenerateTable.Configuration.FillPlaceholder(frame, new Dictionary<string, string>() { { "Placeholder", modelsCode.ToString() } });
            System.IO.File.WriteAllText(System.IO.Path.Combine(this.Config.BaseFilePath, this.Config.ModelFolderName, this.Config.ModelContextName + ".cs"), code, Encoding.UTF8);
        }

        public void SaveRespository()
        {
            var frame = this.Config.GetTemplate("RepositoryFrame.txt");
            var repository = this.Config.GetTemplate("RepositoryTemplate.txt");

            var code = GenerateTable.Configuration.FillPlaceholder(frame, new Dictionary<string, string>() { { "Placeholder", repository } });

            System.IO.File.WriteAllText(System.IO.Path.Combine(this.Config.BaseFilePath, this.Config.RepositoryFolderName, "Repository.cs"), code, Encoding.UTF8);
        }

        public void SaveEntityModels()
        {

            var frame = this.Config.GetTemplate("ModelFrame.txt");
            this.Tables.ForEach(t =>
            {
                var tableCode = GenerateTable.Configuration.GetTemplate(new { RepositoryName = "GenericRepository", t.TableName }, System.IO.Path.Combine(this.Config.TemplateFolderPath, "ModelEntity.txt"));
                var code = GenerateTable.Configuration.FillPlaceholder(frame, new Dictionary<string, string>() { { "Placeholder", tableCode.ToString() } });
                System.IO.File.WriteAllText(System.IO.Path.Combine(this.Config.BaseFilePath, this.Config.ModelFolderName, t.TableName + ".cs"), code, encoding: Encoding.UTF8);
            });

        }


        public void SaveRepositoryModels()
        {

            var frame = this.Config.GetTemplate("RepositoryFrame.txt");
            this.Tables.ForEach(t =>
            {
                var tableCode = GenerateTable.Configuration.GetTemplate(t, System.IO.Path.Combine(this.Config.TemplateFolderPath, "TableRepository.txt"));
                var code = GenerateTable.Configuration.FillPlaceholder(frame, new Dictionary<string, string>() { { "Placeholder", tableCode.ToString() } });
                System.IO.File.WriteAllText(System.IO.Path.Combine(this.Config.BaseFilePath, this.Config.RepositoryFolderName, t.TableName + ".cs"), code, encoding: Encoding.UTF8);
            });

        }
        public class UserTable
        {
            private string _tableName;

            public string TableName
            {
                get { return this._tableName.UpperFirstLetter(); }
                set
                {
                    this._tableName = value;
                }
            }

            public List<TableColumn> Columns { get; set; }
            public List<string> PrimaryKeyNames { get; set; }
            public List<string> IdentityColumnNames { get; set; }
            public UserTable()
            {
                this.TableName = String.Empty;
                this.Columns = new List<TableColumn>();
                this.PrimaryKeyNames = new List<string>();
                this.IdentityColumnNames = new List<string>();
            }
            public void SetIdentityColumnNames(DataContext context)
            {
                this.IdentityColumnNames = context.ExecuteQuery<string>("select Name from sys.identity_columns where object_name(object_id) = {0}", this.TableName).ToList();
            }
            public void SetPrimaryKeyNames(DataContext context)
            {
                this.PrimaryKeyNames = context.ExecuteQuery<string>("SELECT column_name as ColumnName FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE OBJECTPROPERTY(OBJECT_ID(constraint_name), 'IsPrimaryKey') = 1 AND table_name = {0}", this.TableName).ToList();
            }
        }
        public class TableColumn
        {
            public string Name { get; set; }
            public bool IsNullable { get; set; }
            public string DataType { get; set; }
            public string DbType { get; set; }
            public bool IsPrimaryKey { get; set; }
            public bool IsDbGenerated { get; set; }
            public int? Length { get; set; }
        }

        public class Configuration
        {
            public string TemplateFolderPath { get; set; }
            public string Connection { get; set; }
            public string BaseFilePath { get; set; }
            public string BaseNamespace { get; set; }

            public string ModelFolderName { get; set; }
            public string ModelNamespace { get; set; }
            public string ModelContextName { get; set; }
            public string ModelConstructorConnection { get; set; }
            public bool SaveIntoMultiFile { get; set; }

            public string RepositoryFolderName { get; set; }
            public string RepositoryNamespace { get; set; }


            public Func<TableColumn, string> ConvertType { get; set; }
            public Func<UserTable, string> HoverClass { get; set; }
            public Func<TableColumn, string> HoverColumn { get; set; }


            public Configuration()
            {
                this.SetDefaultValue();
            }


            public void SetDefaultValue()
            {
                this.ModelFolderName = "Models";
                this.ModelNamespace = "Models";
                this.RepositoryFolderName = "Repositories";
                this.RepositoryNamespace = "Repositories";

                this.SaveIntoMultiFile = false;
                this.HoverColumn = c =>
                {
                    var columnAnnotation = new StringBuilder();

                    //columnAnnotation.AppendLine(String.Format("private "));

                    columnAnnotation.AppendFormat("[System.Data.Linq.Mapping.ColumnAttribute(Name=\"{0}\"", c.Name);
                    if (c.IsPrimaryKey)
                    {
                        columnAnnotation.Append(", IsPrimaryKey=true");
                    }
                    if (c.IsDbGenerated)
                    {
                        columnAnnotation.Append(", IsDbGenerated=true");
                    }
                    if (
                        c.DbType.ToLower() == "ntext" || //ntext
                        (c.DbType.ToLower() == "varchar" && c.Length == -1)  //varchar(max)
                        )
                    {
                        columnAnnotation.Append(", UpdateCheck=UpdateCheck.Never");
                    }
                    var dbType = String.Format(" ,DbType=\"{0}", c.DbType.UpperFirstLetter());

                    if (c.Length > 0 && (c.DbType.ToLower() == "varchar" || c.DbType.ToLower() == "nvarchar"))
                    {
                        dbType += String.Format("({0})", c.Length);
                    }
                    dbType += "\"";
                    columnAnnotation.Append(dbType);

                    columnAnnotation.Append(")]");
                    return columnAnnotation.ToString();
                };
                this.HoverClass = t => "";
                this.ConvertType = c => c.DataType;
            }



            private string Replace(string template)
            {
                return GenerateTable.Configuration.Replace(this, template);

            }

            public string GetTemplate(string templateName)
            {
                return GenerateTable.Configuration.Replace(this, System.IO.File.ReadAllText(System.IO.Path.Combine(this.TemplateFolderPath, templateName)));
            }

            private static string Replace(object obj, string template)
            {
                obj.GetType().GetProperties().ToList().ForEach(p =>
                {
                    template = template.Replace(String.Format("[{0}]", p.Name), p.GetValue(obj, null).ToString());
                });
                return template;
            }
            public static string GetTemplate(object obj, string templateFullPath)
            {
                return GenerateTable.Configuration.Replace(obj, System.IO.File.ReadAllText(templateFullPath));
            }

            public static string FillPlaceholder(string frame, Dictionary<string, string> texts)
            {
                foreach (var text in texts)
                {
                    frame = frame.Replace(GetPlaceholderName(text.Key), text.Value);
                }
                return frame;
            }
            public static string GetPlaceholderName(string name) { return String.Format("//[{0}]", name); }
        }


        private class Context : DataContext
        {
            public Context(string fileOrServerOrConnection)
                : base(fileOrServerOrConnection)
            {
            }

            public Context(string fileOrServerOrConnection, System.Data.Linq.Mapping.MappingSource mapping)
                : base(fileOrServerOrConnection, mapping)
            {
            }

            public Context(IDbConnection connection)
                : base(connection)
            {
            }

            public Context(IDbConnection connection, System.Data.Linq.Mapping.MappingSource mapping)
                : base(connection, mapping)
            {
            }
        }



    }
    public static class Extension
    {
        public static string UpperFirstLetter(this string str)
        {
            if (String.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            var newStr = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                if (i == 0)
                {
                    newStr.Append(str[i].ToString().ToUpper());
                }
                else
                {
                    newStr.Append(str[i]);
                }
            }
            return newStr.ToString();
        }
    }
}
