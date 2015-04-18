using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateDataAccessLayer
{
    class Program
    {
        static void Main(string[] args)
        {
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
            Console.ReadKey();
        }
    }
}
