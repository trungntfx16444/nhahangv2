namespace AdminPage.Models.CustomizeModels
{
    using System.Collections.Generic;

    public class DataTable_request
    {
        public int draw { get; set; }

        public List<Column> columns { get; set; }

        public List<Order> order { get; set; }

        public int start { get; set; }

        public int length { get; set; }

        public Search search { get; set; }

        public class Column
        {
            public int data { get; set; }

            public string name { get; set; }

            public bool searchable { get; set; }

            public bool orderable { get; set; }

            public Search search { get; set; }
        }

        public class Search
        {
            public string value { get; set; }

            public bool regex { get; set; }
        }

        public class Order
        {
            public int column { get; set; }

            public string dir { get; set; }
        }
    }
}