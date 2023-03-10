namespace PKWebShop.Models.CustomizeModels
{
    using System.Collections.Generic;

    public class MenuView
    {
        public menu menu { get; set; }

        public List<MenuView> child { get; set; }
    }

    public class option_value
    {
        public string Code { get; set; }

        public string Value { get; set; }

        public string Data { get; set; }
    }
}