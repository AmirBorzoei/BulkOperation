using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ConsoleApplication1.SalesManagement;

namespace ConsoleApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //using (var connection = new SqlConnection("Data Source=192.168.10.20;Initial Catalog=KF_Sales;User ID=sa;Password=123"))
            using (var connection = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=KF_Sales;Trusted_Connection=True;"))
            {
                var selectCommand = new SqlCommand("SELECT * FROM [SalesManagement].[Item]", connection);
                connection.Open();
                var dataReader = selectCommand.ExecuteReader();
                var items = new List<Item>();
                while (dataReader.Read())
                {
                    var item = new Item
                    {
                        Id = dataReader.GetGuid(0),
                        RowVersion = dataReader.GetFieldValue<byte[]>(1),
                        CreatedDate = !dataReader.IsDBNull(2) ? dataReader.GetDateTime(2) : DateTime.Now,
                        LastChangedDate = !dataReader.IsDBNull(3) ? dataReader.GetDateTime(3) : DateTime.Now,
                        CreatedBy = !dataReader.IsDBNull(4) ? dataReader.GetString(4) : String.Empty,
                        LastChangedBy = !dataReader.IsDBNull(5) ? dataReader.GetString(5) : String.Empty,
                        Title = !dataReader.IsDBNull(6) ? dataReader.GetString(6) : String.Empty,
                        Description = !dataReader.IsDBNull(7) ? dataReader.GetString(7) : String.Empty,
                        GoodsId = !dataReader.IsDBNull(8) ? dataReader.GetGuid(8) : Guid.Empty,
                    };
                    items.Add(item);
                }
                dataReader.Close();

                items.ForEach(i => i.Title += "1");

                var updateTargetProperties = new Expression<Func<Item, object>>[] {p => p.Title};
                var bulkOperationParam = new BulkOperationParam<Item>(connection, items);
                var numberOfRowsAffected = BulkUpdate(bulkOperationParam, updateTargetProperties);

                Console.WriteLine($"NumberOfRowsAffected:\t{numberOfRowsAffected}");
                Console.ReadKey();
            }
        }


        private static int BulkUpdate<T>(BulkOperationParam<T> bulkOperationParam, params Expression<Func<T, dynamic>>[] updateTargetProperties)
        {
            var tempTableName = nameof(BulkUpdate);
            var bulkOperationConfig = GetBulkOperationConfig(bulkOperationParam, tempTableName);
            WriteToServer(bulkOperationParam, bulkOperationConfig);

            var columnsMustBeUpdated = new StringBuilder();
            foreach (var prop in updateTargetProperties)
            {
                MemberExpression memberExpression;
                if (prop.Body.NodeType == ExpressionType.Convert || prop.Body.NodeType == ExpressionType.ConvertChecked)
                {
                    var unaryExpression = prop.Body as UnaryExpression;
                    memberExpression = unaryExpression?.Operand as MemberExpression;
                }
                else
                {
                    memberExpression = (prop.Body as MemberExpression);
                }

                columnsMustBeUpdated.Append($"{memberExpression.Member.Name}=TT.{memberExpression.Member.Name},");
            }

            var query = $@" UPDATE {bulkOperationConfig.DestinationTableName} SET  {columnsMustBeUpdated.ToString().Remove(columnsMustBeUpdated.ToString().Length - 1)}
                                FROM {bulkOperationConfig.DestinationTableName} T 
                                INNER JOIN {bulkOperationConfig.TempTable} TT ON T.Id=TT.Id AND T.RowVersion=TT.RowVersion;";
            var numberOfRowsAffected = ExecuteQuery(bulkOperationParam.Connection, query);

            if (numberOfRowsAffected != bulkOperationParam.Data.Count())
            {
                DropTable(bulkOperationParam.Connection, bulkOperationConfig.TempTable);
                throw new Exception("Number of affected rows are not expected, maybe CONCURRENCY occurred!");
            }

            DropTable(bulkOperationParam.Connection, bulkOperationConfig.TempTable);

            return numberOfRowsAffected;
        }

        private static BulkOperationConfig GetBulkOperationConfig<T>(BulkOperationParam<T> bulkOperationParam, string tempTableName)
        {
            var type = typeof (T);
            var bulkOperationConfig = new BulkOperationConfig
            {
                DestinationTableName = bulkOperationParam.TableName ?? GetTableName(type),
                AllProperties = PropertiesCache.TypePropertiesCache(type)
            };
            if (bulkOperationParam.ExtraColumns != null)
            {
                foreach (var extraColumnsKey in bulkOperationParam.ExtraColumns.Keys)
                {
                    bulkOperationConfig.AllProperties.Add(new CustomPropertyInfo(extraColumnsKey, typeof (object)));
                }
            }

            bulkOperationConfig.AllPropertiesString = GetColumnsStringSqlServer(bulkOperationConfig.AllProperties);
            bulkOperationConfig.TempTable = $"##{bulkOperationConfig.DestinationTableName.Replace(".", "_").Replace("[", string.Empty).Replace("]", string.Empty)}{tempTableName}";


            var columnNames = bulkOperationConfig.AllPropertiesString.Replace(", [RowVersion]", "");
            var command = new SqlCommand(
                $@"SELECT TOP 0 {columnNames} INTO {bulkOperationConfig.TempTable} FROM {bulkOperationConfig.DestinationTableName} target WITH(NOLOCK);",
                bulkOperationParam.Connection as SqlConnection);
            if (bulkOperationParam.Connection.State != ConnectionState.Open)
            {
                bulkOperationParam.Connection.Open();
            }

            command.ExecuteNonQuery();

            var command2 = new SqlCommand($@"ALTER TABLE {bulkOperationConfig.TempTable}
                                            ADD RowVersion binary(8) NULL;", bulkOperationParam.Connection as SqlConnection);
            command2.ExecuteNonQuery();
            
            return bulkOperationConfig;
        }

        private static void WriteToServer<T>(BulkOperationParam<T> bulkOperationParam, BulkOperationConfig config)
        {
            using (var bulkCopy = new SqlBulkCopy(bulkOperationParam.Connection as SqlConnection, SqlBulkCopyOptions.Default, bulkOperationParam.Transaction))
            {
                bulkCopy.BulkCopyTimeout = bulkOperationParam.BulkCopyTimeout;
                bulkCopy.BatchSize = bulkOperationParam.BatchSize;
                bulkCopy.DestinationTableName = config.TempTable;
                bulkCopy.WriteToServer(ToDataTable(bulkOperationParam.Data, config.TempTable, config.AllProperties, bulkOperationParam.ExtraColumns).CreateDataReader());
            }
        }

        private static DataTable ToDataTable<T>(IEnumerable<T> data, string tableName, IList<PropertyInfo> properties, Dictionary<string, Func<T, object>> extraColumns = null)
        {
            var dataTable = new DataTable(tableName);
            foreach (var prop in properties)
            {
                if (prop.Name.Equals("RowVersion"))
                {
                    var c = dataTable.Columns.Add(prop.Name, typeof (byte[]));
                }
                else if (prop.Name.ToLower().EndsWith("id") && prop.Name != "LoanTypeGroupId" ||
                         (tableName == "Workflow.WorkflowProcessInstance" && prop.Name.ToLower().EndsWith("by") && prop.Name.ToLower().EndsWith("by")))
                {
                    dataTable.Columns.Add(prop.Name, typeof (Guid));
                }
                else
                {
                    dataTable.Columns.Add(prop.Name);
                }
            }

            var typeCasts = new Type[properties.Count];
            for (var i = 0; i < properties.Count; i++)
            {
                var isEnum = properties[i].PropertyType.IsEnum;
                if (isEnum)
                {
                    typeCasts[i] = Enum.GetUnderlyingType(properties[i].PropertyType);
                }
                else
                {
                    typeCasts[i] = null;
                }
            }

            foreach (var item in data)
            {
                try
                {
                    var values = new object[properties.Count];
                    for (var i = 0; i < properties.Count; i++)
                    {
                        if (extraColumns != null && extraColumns.ContainsKey(properties[i].Name))
                        {
                            var value = extraColumns[properties[i].Name];
                            values[i] = value.Invoke(item);
                        }
                        else
                        {
                            var value = properties[i].GetValue(item, null);
                            var castToType = typeCasts[i];
                            values[i] = castToType == null ? value : Convert.ChangeType(value, castToType);
                        }
                    }

                    dataTable.Rows.Add(values);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            return dataTable;
        }

        private static void DropTable(IDbConnection connection, string tableName)
        {
            var query = $@"DROP TABLE {tableName};";
            ExecuteQuery(connection, query);
        }

        private static int ExecuteQuery(IDbConnection connection, string query)
        {
            var command = new SqlCommand { CommandText = query, Connection = connection as SqlConnection };
            var numberOfRowsAffected = command.ExecuteNonQuery();
            //connection.Close();
            return numberOfRowsAffected;
        }

        private static string GetTableName(Type type)
        {
            var tableName = type.Name;
            var schemaName = type.Namespace?.Split('.')[1];
            var tableNameWithSchema = $"[{schemaName}].[{tableName}]";

            return tableNameWithSchema;
        }

        private static string GetColumnsStringSqlServer(IEnumerable<PropertyInfo> properties, string tablePrefix = null)
        {
            return string.Join(", ", properties.Select(property => $"{tablePrefix}[{property.Name}]"));
        }
    }
}