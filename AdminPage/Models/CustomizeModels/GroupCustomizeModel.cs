namespace AdminPage.Models.CustomizeModels
{
    using System.Collections.Generic;

    public class Group_News
    {
        public n_news news { get; set; }

        public List<string> langs { get; set; }
    }

    public class Group_Service
    {
        public service service { get; set; }

        public List<string> langs { get; set; }
    }

    public class Group_Product
    {
        public product product { get; set; }

        public List<string> langs { get; set; }
    }
}