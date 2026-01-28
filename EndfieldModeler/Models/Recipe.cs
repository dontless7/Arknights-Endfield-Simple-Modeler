namespace EndfieldModeler.Models
{
    public class Recipe
    {
        public string ItemName { get; set; } = "";
        public string MachineName { get; set; } = "";
        public float CraftingTimeSeconds { get; set; }
        public float OutputAmount { get; set; } = 1;
        public List<Ingredient> Inputs { get; set; } = new List<Ingredient>();
        public bool IsRawResource { get; set; }
        public float PowerConsumption { get; set; }
    }
}
