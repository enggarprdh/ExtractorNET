using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace extrator
{
   public class SqlManager
   {
      protected string _connStr { get; set; }

      public SqlManager(string connStr)
      {
         _connStr = connStr;
      }

      public void CreateTable()
      {
         var q = @"IF OBJECT_ID('UploadFileExtractor') IS NULL 
                            BEGIN
                               CREATE TABLE[dbo].[UploadFileExtractor](
                                 [ID][uniqueidentifier] NULL,
				                     [PathFile] [varchar](255) NULL,
				                     [ModDate] [datetime] NULL,
                                ) ON[PRIMARY]
                            END";
         SqlConnection conn = new SqlConnection(this._connStr);
         conn.Open();
         SqlCommand com = new SqlCommand(q, conn);
         com.ExecuteNonQuery();
         conn.Close();
      }

      public void InsertToTableUploadFileExtractor(string pathFile)
      {
         SqlConnection conn = new SqlConnection(this._connStr);
         conn.Open();
         var q = "INSERT INTO UploadFileExtractor VALUES (@ID, @PathFile, @ModDate)";
         SqlCommand com = new SqlCommand(q, conn);
         com.Parameters.AddWithValue("@ID", Guid.NewGuid());
         com.Parameters.AddWithValue("@PathFile", pathFile);
         com.Parameters.AddWithValue("@ModDate", DateTime.Now);
         com.ExecuteNonQuery();
         conn.Close();
      }
   }
}
