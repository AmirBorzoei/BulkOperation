using System.Collections.Generic;
using System.Reflection;

namespace ConsoleApplication1
{
    public class BulkOperationConfig
    {
        public List<PropertyInfo> AllProperties { get; set; }

        public string AllPropertiesString { get; set; }

        public string TempTable { get; set; }

        public string DestinationTableName { get; set; }
    }
}