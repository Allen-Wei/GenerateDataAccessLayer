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
            var connenction = "server=192.168.11.80; uid=sa; pwd=huao@123; database=Ysd_Credit";

            GenerateTable generate = new GenerateTable(new GenerateTable.Configuration()
            {
                TemplateFolderPath = @"E:\Projects\GenerateDataAccessLayer\GenerateDataAccessLayer\GenerateTableTemplates",
                BaseFilePath = @"E:\Temporary\Generate",
                BaseNamespace = "Ysd",
                ModelNamespace = "DataAccessLayer.Models",
                RepositoryNamespace = "BusinessLogicLayer.Repository",
                Connection = connenction,
                ModelContextName = "ModelContext",
                ModelConstructorConnection = "System.Web.Configuration.WebConfigurationManager.ConnectionStrings[\"ConnectionString\"].ConnectionString"
            });
            generate.SetEntities();

            generate.SaveModels();

            generate.SaveEntityModels();
            generate.SaveRepositoryModels();
            generate.SaveRespository();
        }
    }
}
