using EndfieldModeler.Models;

namespace EndfieldModeler.Nodes
{
    public class ProductionNode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Recipe Recipe { get; set; } = new Recipe();
        public float TargetItemsPerMinute { get; set; }
        public Point Location { get; set; }
        public Size Size { get; set; } = new Size(280, 130);
        public List<ProductionNode> InputNodes { get; set; } = new List<ProductionNode>();
        public Dictionary<Guid, Point> LabelOffsets { get; set; } = new Dictionary<Guid, Point>();

        public void UpdatePredecessors()
        {
            foreach (var prev in InputNodes)
            {
                var ingredient = Recipe.Inputs.FirstOrDefault(i => i.Name == prev.Recipe.ItemName);
                if (ingredient != null)
                {
                    prev.TargetItemsPerMinute = (this.TargetItemsPerMinute / this.Recipe.OutputAmount) * ingredient.Amount;
                    prev.UpdatePredecessors();
                }
            }
        }

        public float GetExactMachines()
        {
            if (Recipe.IsRawResource || Recipe.CraftingTimeSeconds <= 0) return 0;
            float itemsPerMachinePerMin = (60f / Recipe.CraftingTimeSeconds) * Recipe.OutputAmount;
            return TargetItemsPerMinute / itemsPerMachinePerMin;
        }
    }
}
