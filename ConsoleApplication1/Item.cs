using System;

namespace ConsoleApplication1.SalesManagement
{
    public class Item
    {
        public Guid Id { get; set; }

        public byte[] RowVersion { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? LastChangedDate { get; set; }

        public string CreatedBy { get; set; }

        public string LastChangedBy { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public Guid GoodsId { get; set; }
    }
}