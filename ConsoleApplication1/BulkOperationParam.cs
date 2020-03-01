using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace ConsoleApplication1
{
    public class BulkOperationParam<T>
    {
        public BulkOperationParam(
            IDbConnection connection,
            IEnumerable<T> data,
            string tableName = null,
            Dictionary<string, Func<T, object>> extraColumns = null,
            SqlTransaction transaction = null,
            int batchSize = 0,
            int bulkCopyTimeout = 600)
        {
            Connection = connection;
            Data = data;
            TableName = tableName;
            ExtraColumns = extraColumns;
            Transaction = transaction;
            BatchSize = batchSize;
            BulkCopyTimeout = bulkCopyTimeout;
        }

        public IDbConnection Connection { get; set; }

        public IEnumerable<T> Data { get; set; }

        public string TableName { get; set; }

        public Dictionary<string, Func<T, object>> ExtraColumns { get; set; }

        public SqlTransaction Transaction { get; set; }

        public int BatchSize { get; set; }

        public int BulkCopyTimeout { get; set; }
    }
}