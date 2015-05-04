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
            var connenction = "server=192.168.11.88; uid=sa; pwd=huao@123; database=Ysd_Credit";

            GenerateTable generate = new GenerateTable(new GenerateTable.Configuration()
            {
                TemplateFolderPath = @"D:\Projects\GenerateDataAccessLayer\GenerateDataAccessLayer\GenerateTableTemplates",
                BaseFilePath = @"E:\Temporary\Generate",
                BaseNamespace = "Ysd",
                ModelNamespace = "DataAccessLayer",
                RepositoryNamespace = "BusinessLogicLayer",
                Connection = connenction,
                ModelContextName = "ModelContext",
                ModelConstructorConnection = "server=192.168.11.88; uid=sa; pwd=huao@123; database=Ysd_Credit"
            });
            generate.SetEntities();
            generate.SaveModels();
            generate.SaveRespository();
            generate.SaveEntityModels();
            generate.SaveRepositoryModels();
        }
    }
}
