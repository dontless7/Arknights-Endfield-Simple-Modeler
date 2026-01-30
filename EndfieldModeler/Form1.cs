using EndfieldModeler.Models;
using EndfieldModeler.Nodes;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;

namespace EndfieldModeler
{
    public partial class Form1 : Form
    {
        private List<Recipe> _recipes = new List<Recipe>();
        private List<ProductionNode> _nodes = new List<ProductionNode>();
        
        private Dictionary<string, Image> _iconCache = new Dictionary<string, Image>();
        private Font _fontBold = new Font("Segoe UI", 9, FontStyle.Bold);
        private Font _fontSmall = new Font("Segoe UI", 8);
        private Font _fontSmallBold = new Font("Segoe UI", 8, FontStyle.Bold);

        private PointF _viewOffset = new PointF(0, 0);
        private float _zoom = 1.0f;
        private Point _lastMousePos;
        private bool _isPanning = false;
        private ProductionNode? _draggedNode = null;
        private (Guid nodeId, Guid prevId)? _draggedLabel = null;

        private Stack<List<Point>> _undoStack = new Stack<List<Point>>();
        private Stack<List<Point>> _redoStack = new Stack<List<Point>>();

        private Panel _searchPanel = null!;
        private TextBox _searchBox = null!;
        private FlowLayoutPanel _searchList = null!;

        private Button _btnCreateProduction;

        private bool _isDraggingScroll = false;
        private int _dragStartY;
        private int _dragStartScrollVal;

        public Form1()
        {
            DoubleBuffered = true;
            Size = new Size(1400, 900);
            BackColor = Color.FromArgb(20, 20, 25);
            Text = "Arknights:Endfield Simple Modeler (AESM) v1.4.1-alpha";
            KeyPreview = true;

            InitializeRecipes();
            InitializeSearchMenu();

            _btnCreateProduction = new Button
            {
                Text = "Create new Production-Line",
                Size = new Size(250, 40),
                BackColor = Color.FromArgb(45, 45, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnCreateProduction.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 90);

            _btnCreateProduction.Location = new Point((this.ClientSize.Width - _btnCreateProduction.Width) / 2, 20);

            _btnCreateProduction.Click += (s, e) => {
                _searchPanel.Visible = !_searchPanel.Visible;
                if (_searchPanel.Visible)
                {
                    _searchPanel.Location = new Point(_btnCreateProduction.Location.X, _btnCreateProduction.Bottom + 5);
                    _searchBox.Text = "";
                    UpdateSearchList("");
                    _searchBox.Focus();
                }
            };
            Controls.Add(_btnCreateProduction);

            this.Resize += (s, e) => {
                _btnCreateProduction.Location = new Point((this.ClientSize.Width - _btnCreateProduction.Width) / 2, 20);
                _searchPanel.Location = new Point(_btnCreateProduction.Location.X, _btnCreateProduction.Bottom + 5);
            };

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
            Paint += OnPaint;
            KeyDown += OnKeyDown;
        }

        private void InitializeRecipes()
        {
            var powerValues = new Dictionary<string, float>
            {
                { "Planting Unit", 20f },
                { "Seed-Picking Unit", 10f },
                { "Refining Unit", 5f },
                { "Shredding Unit", 5f },
                { "Grinding Unit", 50f },
                { "Moulding Unit", 10f },
                { "Fitting Unit", 20f },
                { "Gearing Unit", 10f },
                { "Packaging Unit", 20f },
                { "Filling Unit", 20f },
                { "World resource", 0f },
                { "Forge of the Sky", 50f },
                { "Fluid Pump", 10f },
                { "Reactor Crucible", 50 }
            };

            void AddRecipe(string name, string machine, float timeInSeconds, float outputAmount, params (string, float)[] ins)
            {
                float watts = powerValues.ContainsKey(machine) ? powerValues[machine] : 0;
                _recipes.Add(new Recipe
                {
                    ItemName = name,
                    MachineName = machine,
                    CraftingTimeSeconds = timeInSeconds,
                    OutputAmount = outputAmount,
                    PowerConsumption = watts,
                    Inputs = ins.Select(x => new Ingredient { Name = x.Item1, Amount = x.Item2 }).ToList()
                });
            }

            void AddRaw(string name) => _recipes.Add(new Recipe { ItemName = name, MachineName = "World resource", IsRawResource = true, PowerConsumption = 0 });

            // Natural Resources
            AddRecipe("Buckflower", "Planting Unit", 2, 1, ("Buckflower Seed", 1));
            AddRecipe("Buckflower Seed", "Seed-Picking Unit", 2, 2, ("Buckflower", 1));
            AddRecipe("Sandleaf", "Planting Unit", 2, 1, ("Sandleaf Seed", 1));
            AddRecipe("Sandleaf Seed", "Seed-Picking Unit", 2, 2, ("Sandleaf", 1));
            AddRecipe("Citrome", "Planting Unit", 2, 1, ("Citrome Seed", 1));
            AddRecipe("Citrome Seed", "Seed-Picking Unit", 2, 2, ("Citrome", 1));
            AddRecipe("Aketine", "Planting Unit", 2, 1, ("Aketine Seed", 1));
            AddRecipe("Aketine Seed", "Seed-Picking Unit", 2, 2, ("Aketine", 1));
                // Jincao (not automatable)
            AddRecipe("Jincao Seed", "Seed-Picking Unit", 2, 2, ("Jincao", 1));
                // Yazhen (not automatable)
            AddRecipe("Yazhen Seed", "Seed-Picking Unit", 2, 2, ("Yazhen", 1));
            AddRecipe("Clean Water", "Fluid Pump", 1, 1, ("", 0));
            AddRecipe("Reed Rye Seed", "Seed-Picking Unit", 2, 2, ("Reed Rye", 1));
            AddRecipe("Tartpepper Seed", "Seed-Picking Unit", 2, 2, ("Tartpepper", 1));
                // Natural Resources (Raw)
                AddRaw("Originium Ore");
                AddRaw("Amethyst Ore");
                AddRaw("Ferrium Ore");
                AddRaw("Reed Rye");
                AddRaw("Jincao");
                AddRaw("Yazhen");

            // AIC Products
            AddRecipe("Jincao Solution", "Reactor Crucible", 2, 1, ("Clean Water", 1), ("Jincao Powder", 1));
            AddRecipe("Yazhen Solution", "Reactor Curcible", 2, 1, ("Clean Water", 1), ("Yazhen Powder", 1));
            AddRecipe("Liquid Xiranite", "Reactor Crucible", 2, 1, ("Clean Water", 1), ("Xiranite", 1));
            AddRecipe("Carbon", "Refining Unit", 2, 1, ("Buckflower", 1));
            AddRecipe("Carbon", "Refining Unit", 2, 1, ("Sandleaf", 1));
            AddRecipe("Origocrust", "Refining Unit", 2, 1, ("Originium Ore", 1));
            AddRecipe("Amethyst Fiber", "Refining Unit", 2, 1, ("Amethyst Ore", 1));
            AddRecipe("Ferrium", "Refining Unit", 2, 1, ("Ferrium Ore", 1));
            AddRecipe("Stabilized Carbon", "Refining Unit", 2, 1, ("Dense Carbon Powder", 1));
            AddRecipe("Packed Origocrust", "Refining Unit", 2, 1, ("Dense Origocrust Powder", 1));
            AddRecipe("Cryston Fiber", "Refining Unit", 2, 1, ("Cryston Powder", 1));
            AddRecipe("Steel", "Refining Unit", 2, 1, ("Dense Ferrium Powder", 1));
            AddRecipe("Xiranite", "Forge of the Sky", 2, 1, ("Clean Water", 1), ("Stabilized Carbon", 2));
            AddRecipe("Carbon Powder", "Refining Unit", 2, 2, ("Sandleaf Powder", 3));
            AddRecipe("Carbon Powder", "Shredding Unit", 2, 2, ("Carbon", 1));
            AddRecipe("Originium Powder", "Shredding Unit", 2, 1, ("Originium Ore", 1));
            AddRecipe("Origocrust Powder", "Shredding Unit", 2, 1, ("Origocrust", 1));
            AddRecipe("Amethyst Powder", "Shredding Unit", 2, 1, ("Amethyst Fiber", 1));
            AddRecipe("Ferrium Powder", "Shredding Unit", 2, 1, ("Ferrium", 1));
            AddRecipe("Sandleaf Powder", "Shredding Unit", 2, 3, ("Sandleaf", 1));
            AddRecipe("Aketine Powder", "Shredding Unit", 2, 2, ("Aketine", 1));
            AddRecipe("Ground Buckflower Powder", "Grinding Unit", 2, 1, ("Sandleaf Powder", 1), ("Buckflower Powder", 2));
            AddRecipe("Ground Citrome Powder", "Grinding Unit", 2, 1, ("Sandleaf Powder", 1), ("Citrome Powder", 2));
            AddRecipe("Dense Carbon Powder", "Grinding Unit", 2, 1, ("Sandleaf Powder", 1), ("Carbon Powder", 2));
            AddRecipe("Dense Originium Powder", "Grinding Unit", 2, 1, ("Sandleaf Powder", 1), ("Originium Powder", 2));
            AddRecipe("Dense Origocrust Powder", "Refining Unit", 2, 1, ("Dense Originium Powder", 1));
            AddRecipe("Cryston Powder", "Grinding Unit", 2, 1, ("Sandleaf Powder", 1), ("Amethyst Powder", 2));
            AddRecipe("Dense Ferrium Powder", "Grinding Unit", 2, 1, ("Sandleaf Powder", 1), ("Ferrium Powder", 2));
            AddRecipe("Amethyst Bottle", "Moulding Unit", 2, 1, ("Amethyst Fiber", 2));
            AddRecipe("Ferrium Bottle", "Moulding Unit", 2, 1, ("Ferrium", 2));
            AddRecipe("Cryston Bottle", "Moulding Unit", 2, 1, ("Cryston Fiber", 2));
            AddRecipe("Steel Bottle", "Moulding Unit", 2, 1, ("Steel", 2));
            AddRecipe("Amethyst Part", "Fitting Unit", 2, 1, ("Amethyst Fiber", 1));
            AddRecipe("Ferrium Part", "Fitting Unit", 2, 1, ("Ferrium", 1));
            AddRecipe("Cryston Part", "Fitting Unit", 2, 1, ("Cryston Fiber", 1));
            AddRecipe("Steel Part", "Fitting Unit", 2, 1, ("Steel", 1));
            AddRecipe("Amethyst Component", "Gearing Unit", 10, 1, ("Amethyst Fiber", 5), ("Origocrust", 5));
            AddRecipe("Ferrium Component", "Gearing Unit", 10, 1, ("Ferrium", 10), ("Origocrust", 10));
            AddRecipe("Cryston Component", "Gearing Unit", 10, 1, ("Packed Origocrust", 10), ("Cryston Fiber", 10));
            AddRecipe("Xiranite Component", "Gearing Unit", 10, 1, ("Xiranite", 10), ("Packed Origocrust", 10));
            AddRecipe("LC Valley Battery", "Packaging Unit", 10, 1, ("Originium Powder", 10), ("Amethyst Part", 5));
            AddRecipe("SC Valley Battery", "Packaging Unit", 10, 1, ("Originium Powder", 15), ("Ferrium Part", 10));
            AddRecipe("HC Valley Battery", "Packaging Unit", 10, 1, ("Steel Part", 10), ("Dense Originium Powder", 15));
            AddRecipe("LC Wuling Battery", "Packaging Unit", 10, 1, ("Dense Originium Powder", 15), ("Xiranite", 5));
            AddRecipe("Ferrium Bottle (Clean Water)", "Filling Unit", 2, 1, ("Clean Water", 1), ("Ferrium Bottle", 1));
            AddRecipe("Ferrium Bottle (Jincao)", "Filling Unit", 2, 1, ("Jincao Solution", 1), ("Ferrium Bottle", 1));
            AddRecipe("Ferrium Bottle (Yazhen)", "Filling Unit", 2, 1, ("Yazhen Solution", 1), ("Ferrium Bottle", 1));
            AddRecipe("Ferrium Bottle (Liquid Xiranite)", "Filling Unit", 2, 1, ("Liquid Xiranite", 1), ("Ferrium Bottle", 1));

            // Usable Items
            AddRecipe("Industrial Explosive", "Packaging Unit", 10, 1, ("Aketine Powder", 1), ("Amethyst Part", 5));
            AddRecipe("Buckflower Powder", "Shredding Unit", 2, 2, ("Buckflower", 1));
            AddRecipe("Citrome Powder", "Shredding Unit", 2, 2, ("Citrome", 1));
            AddRecipe("Jincao Powder", "Shredding Unit", 2, 2, ("Jincao", 1));
            AddRecipe("Yazhen Powder", "Shredding Unit", 2, 2, ("Yazhen", 1));
                // Firebuckle Powder        (Not automatable)
                // Citromix                 (Not automatable)
                // Fluffed Jincao Powder    (Not automatable)
                // Thorny Yazhen Powder     (Not automatable)
            AddRecipe("Buck Capsule [C]", "Filling Unit", 10, 1, ("Buckflower Powder", 5), ("Amethyst Bottle", 5));
            AddRecipe("Buck Capsule [B]", "Filling Unit", 10, 1, ("Buckflower Powder", 10), ("Ferrium Bottle", 10));
            AddRecipe("Canned Citrome [C]", "Filling Unit", 10, 1, ("Citrome Powder", 5), ("Amethyst Bottle", 5));
            AddRecipe("Canned Citrome [B]", "Filling Unit", 10, 1, ("Citrome Powder", 10), ("Ferrium Bottle", 10));
            AddRecipe("Jincao Drink", "Packaging Unit", 10, 1, ("Ferrium Bottle (Jincao)", 5), ("Ferrium Part", 10));
            AddRecipe("Yazhen Syringe [C]", "Packaging Unit", 10, 1, ("Ferrium Bottle (Yazhen)", 5), ("Ferrium Part", 10));
            AddRecipe("Buck Capsule [A]", "Filling Unit", 10, 1, ("Steel Bottle", 10), ("Ground Buckflower Powder", 10));
            AddRecipe("Canned Citrome [A]", "Filling Unit", 10, 1, ("Ground Citrome Powder", 10), ("Steel Bottle", 10));
                // Buckpill [S]             (Not automatable)
                // Arts Vial                (Not automatable)
                // Kunst Vial               (Not automatable)
                // Arts Tube                (Not automatable)
                // Meaty Buckflower Stew    (Not automatable)
                // Handmade Weirdrop        (Not automatable)
                // Sesqa Style Fillet       (Not automatable)
                // Instant Bone Soup        (Not automatable)
                // Stew Meeting             (Not automatable)
                // Mini Honey Slugpudding   (Not automatable)
                // Sod-Turning Meat Soup    (Not automatable)
                // Jakubs Legacy            (Not automatable)
                // Cartilage Tack           (Not automatable)
                // Edible Denstack          (Not automatable)
                // Hazefyre Blossom         (Not automatable)
                // Old Man Johns Burger     (Not automatable)
                // Cosmo-Melto Jelly        (Not automatable)
                // Hub Emergency Ration     (Not automatable)
                // Superhot Fruit Preserves (Not automatable)

            //ignore();
        }

        private void InitializeSearchMenu()
        {
            _searchPanel = new Panel
            {
                Size = new Size(450, 500),
                BackColor = Color.FromArgb(25, 25, 30),
                Visible = false,
                Padding = new Padding(1)
            };

            _searchPanel.Paint += (s, e) => {
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(60, 60, 70), 2), 0, 0, _searchPanel.Width - 1, _searchPanel.Height - 1);
            };

            _searchBox = new TextBox
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(20, 20, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle
            };
            _searchBox.TextChanged += (s, e) => UpdateSearchList(_searchBox.Text);

            Panel listContainer = new Panel
            {
                Location = new Point(0, _searchBox.Bottom),
                Size = new Size(435, _searchPanel.Height - _searchBox.Height - 2),
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0),
            };

            _searchList = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                Size = new Size(listContainer.Width + 20, listContainer.Height),
                AutoScroll = true,
                BackColor = Color.FromArgb(25, 25, 30),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10, 10, 0, 10)
            };

            Panel customScrollTrack = new Panel
            {
                Width = 10,
                Height = listContainer.Height,
                Location = new Point(_searchPanel.Width - 12, _searchBox.Bottom),
                BackColor = Color.FromArgb(30, 30, 35),
                Cursor = Cursors.Default
            };

            Panel scrollThumb = new Panel
            {
                Width = 6,
                Height = 100,
                BackColor = Color.FromArgb(80, 80, 90),
                Location = new Point(2, 0),
                Cursor = Cursors.Hand
            };

            Action syncThumb = () => {
                if (_searchList.DisplayRectangle.Height <= _searchList.Height)
                {
                    scrollThumb.Visible = false;
                    return;
                }
                scrollThumb.Visible = true;
                float scrollRange = _searchList.DisplayRectangle.Height - _searchList.Height;
                float thumbRange = customScrollTrack.Height - scrollThumb.Height;
                float scrollPercent = (float)-_searchList.AutoScrollPosition.Y / scrollRange;
                scrollThumb.Top = (int)(scrollPercent * thumbRange);
            };

            _searchList.Scroll += (s, e) => syncThumb();
            _searchList.MouseWheel += (s, e) => syncThumb();

            scrollThumb.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    _isDraggingScroll = true;
                    _dragStartY = e.Y;
                    _dragStartScrollVal = -_searchList.AutoScrollPosition.Y;
                }
            };

            scrollThumb.MouseMove += (s, e) => {
                if (_isDraggingScroll)
                {
                    int deltaY = e.Y - _dragStartY;
                    float thumbRange = customScrollTrack.Height - scrollThumb.Height;
                    float scrollRange = _searchList.DisplayRectangle.Height - _searchList.Height;
                    float scrollDelta = (deltaY / thumbRange) * scrollRange;
                    _searchList.AutoScrollPosition = new Point(0, (int)(_dragStartScrollVal + scrollDelta));
                    syncThumb();
                }
            };

            scrollThumb.MouseUp += (s, e) => _isDraggingScroll = false;
            scrollThumb.MouseEnter += (s, e) => scrollThumb.BackColor = Color.FromArgb(110, 110, 125);
            scrollThumb.MouseLeave += (s, e) => scrollThumb.BackColor = Color.FromArgb(80, 80, 90);

            listContainer.Controls.Add(_searchList);
            customScrollTrack.Controls.Add(scrollThumb);

            _searchPanel.Controls.Add(listContainer);
            _searchPanel.Controls.Add(customScrollTrack);
            _searchPanel.Controls.Add(_searchBox);
            Controls.Add(_searchPanel);
        }

        private void UpdateSearchList(string filter)
        {
            _searchList.SuspendLayout();
            _searchList.Controls.Clear();

            var matches = _recipes
                .Where(r => !r.IsRawResource)
                .Where(r => string.IsNullOrWhiteSpace(filter) || r.ItemName.ToLower().Contains(filter.ToLower()))
                .OrderBy(r => r.ItemName);

            foreach (var r in matches)
            {
                Panel itemPanel = new Panel
                {
                    Width = 410,
                    Height = 75,
                    Margin = new Padding(0, 0, 0, 6),
                    BackColor = Color.FromArgb(40, 40, 45),
                    Cursor = Cursors.Hand
                };

                Action selectAction = () => {
                    CreateFullTree(r);
                    _searchPanel.Visible = false;
                };

                Action applyHover = () => itemPanel.BackColor = Color.FromArgb(60, 60, 70);
                Action removeHover = () => itemPanel.BackColor = Color.FromArgb(40, 40, 45);

                PictureBox itemIcon = new PictureBox
                {
                    Image = GetIcon(r.ItemName),
                    Size = new Size(42, 42),
                    Location = new Point(12, 16),
                    SizeMode = PictureBoxSizeMode.Zoom
                };

                Label nameLabel = new Label
                {
                    Text = r.ItemName,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Location = new Point(65, 12),
                    AutoSize = true
                };

                PictureBox machIcon = new PictureBox
                {
                    Image = GetIcon(r.MachineName),
                    Size = new Size(22, 22),
                    Location = new Point(68, 38),
                    SizeMode = PictureBoxSizeMode.Zoom
                };

                Label machLabel = new Label
                {
                    Text = r.MachineName,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9f),
                    Location = new Point(94, 40),
                    AutoSize = true
                };

                itemPanel.Click += (s, e) => selectAction();
                itemIcon.Click += (s, e) => selectAction();
                nameLabel.Click += (s, e) => selectAction();
                machIcon.Click += (s, e) => selectAction();
                machLabel.Click += (s, e) => selectAction();
                itemPanel.MouseEnter += (s, e) => applyHover();
                itemPanel.MouseLeave += (s, e) => {
                    if (!itemPanel.ClientRectangle.Contains(itemPanel.PointToClient(Cursor.Position)))
                        removeHover();
                };
                itemIcon.MouseEnter += (s, e) => applyHover();
                nameLabel.MouseEnter += (s, e) => applyHover();
                machIcon.MouseEnter += (s, e) => applyHover();
                machLabel.MouseEnter += (s, e) => applyHover();

                itemPanel.Controls.Add(itemIcon);
                itemPanel.Controls.Add(nameLabel);
                itemPanel.Controls.Add(machIcon);
                itemPanel.Controls.Add(machLabel);

                int startX = itemPanel.Width - 40;
                foreach (var ing in r.Inputs)
                {
                    PictureBox ingIcon = new PictureBox
                    {
                        Image = GetIcon(ing.Name),
                        Size = new Size(28, 28),
                        Location = new Point(startX, 23),
                        SizeMode = PictureBoxSizeMode.Zoom
                    };
                    ingIcon.Click += (s, e) => selectAction();
                    ingIcon.MouseEnter += (s, e) => applyHover();
                    itemPanel.Controls.Add(ingIcon);
                    startX -= 32;
                }

                _searchList.Controls.Add(itemPanel);
            }
            _searchList.ResumeLayout();
            _searchList.PerformLayout();
        }

        private void ignore()
        {
            Debug.WriteLine("================================");

            foreach (Recipe r in _recipes)
            {
                if (GetIcon(r.ItemName) == null)
                {
                    Debug.WriteLine(r.ItemName);
                }
            }

            Debug.WriteLine("================================");
        }

        private Image? GetIcon(string name)
        {
            if (_iconCache.TryGetValue(name, out var img)) return img;

            string cleanName = name
                .Replace("-", "_")
                .Replace(" ", "_")
                .Replace("[", "")
                .Replace("]", "")
                .Replace("(", "")
                .Replace(")", "")
                .ToLower() + ".png";

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] paths = {
                Path.Combine(baseDir, "Item Files", "AIC Products", cleanName),
                Path.Combine(baseDir, "Item Files", "Natural Resources", cleanName),
                Path.Combine(baseDir, "Item Files", "Facilities", cleanName),
                Path.Combine(baseDir, "Item Files", "Usable Items", cleanName)
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    var loadedImg = Image.FromFile(path);
                    _iconCache[name] = loadedImg;
                    return loadedImg;
                }
            }
            return null;
        }

        private void SaveUndo()
        {
            _undoStack.Push(_nodes.Select(n => n.Location).ToList());
            if (_undoStack.Count > 10) _undoStack = new Stack<List<Point>>(_undoStack.Take(10).Reverse());
            _redoStack.Clear();
        }

        private void CreateFullTree(Recipe rootRecipe)
        {
            _nodes.Clear();
            _viewOffset = new PointF(0, 0);
            _zoom = 1.0f;
            float baseRate = (60f / (rootRecipe.CraftingTimeSeconds > 0 ? rootRecipe.CraftingTimeSeconds : 1)) * rootRecipe.OutputAmount;

            int calcHeight = Math.Max(160, 100 + (rootRecipe.Inputs.Count * 30));
            var root = new ProductionNode
            {
                Recipe = rootRecipe,
                Location = new Point(Width / 2 + 200, Height / 2),
                TargetItemsPerMinute = baseRate,
                Size = new Size(300, calcHeight)
            };

            _nodes.Add(root);
            GeneratePredecessors(root, new HashSet<string> { root.Recipe.ItemName });
            Invalidate();
        }

        private void GeneratePredecessors(ProductionNode parent, HashSet<string> visited)
        {
            int i = 0;
            int total = parent.Recipe.Inputs.Count;

            foreach (var ing in parent.Recipe.Inputs)
            {
                var rec = _recipes.FirstOrDefault(r => r.ItemName == ing.Name);
                if (rec == null) continue;

                float needed = (parent.TargetItemsPerMinute / parent.Recipe.OutputAmount) * ing.Amount;

                int xPos = parent.Location.X - 450;
                int yOff = (i - (total / 2)) * 250;
                Point targetLoc = new Point(xPos, parent.Location.Y + yOff);

                targetLoc = GetFreeLocation(targetLoc);

                int calcHeight = Math.Max(160, 100 + (rec.Inputs.Count * 30));
                var node = new ProductionNode
                {
                    Recipe = rec,
                    Location = targetLoc,
                    TargetItemsPerMinute = needed,
                    Size = new Size(300, calcHeight)
                };

                parent.InputNodes.Add(node);
                _nodes.Add(node);

                if (!visited.Contains(ing.Name) && !rec.IsRawResource)
                {
                    GeneratePredecessors(node, new HashSet<string>(visited) { ing.Name });
                }
                i++;
            }
        }

        private Point GetFreeLocation(Point desiredLoc)
        {
            Point safeLoc = desiredLoc;
            bool occupied = true;
            while (occupied)
            {
                occupied = false;
                foreach (var existingNode in _nodes)
                {
                    Rectangle existingRect = new Rectangle(existingNode.Location, existingNode.Size);
                    Rectangle newRect = new Rectangle(safeLoc, new Size(300, 250));

                    if (existingRect.IntersectsWith(newRect))
                    {
                        safeLoc.Y += existingRect.Height + 20;
                        occupied = true;
                        break;
                    }
                }
            }
            return safeLoc;
        }

        private PointF ScreenToWorld(Point p) => new PointF((p.X - _viewOffset.X) / _zoom, (p.Y - _viewOffset.Y) / _zoom);

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            _lastMousePos = e.Location;
            PointF worldPos = ScreenToWorld(e.Location);

            if (_searchPanel.Visible)
            {
                if (!_searchPanel.Bounds.Contains(e.Location) && !_btnCreateProduction.Bounds.Contains(e.Location))
                {
                    _searchPanel.Visible = false;
                }
            }

            if (e.Button == MouseButtons.Middle)
            {
                _isPanning = true;
                return;
            }

            var hit = _nodes.LastOrDefault(n => {
                Rectangle rect = new Rectangle(n.Location, n.Size);
                bool isRoot = !_nodes.Any(o => o.InputNodes.Contains(n));

                if (isRoot)
                {
                    rect.Width += 40;
                }

                return rect.Contains((int)worldPos.X, (int)worldPos.Y);
            });

            if (hit != null)
            {
                bool isRoot = !_nodes.Any(o => o.InputNodes.Contains(hit));
                if (isRoot)
                {
                    int relX = (int)worldPos.X - hit.Location.X;
                    int relY = (int)worldPos.Y - hit.Location.Y;

                    if (relX > hit.Size.Width + 5)
                    {
                        float itemsPerCycle = (60f / hit.Recipe.CraftingTimeSeconds) * hit.Recipe.OutputAmount;

                        if (relY >= 0 && relY <= 30)
                        {
                            hit.TargetItemsPerMinute += itemsPerCycle;
                            hit.UpdatePredecessors();
                            Invalidate();
                            return;
                        }
                        else if (relY >= 35 && relY <= 65)
                        {
                            if (hit.TargetItemsPerMinute > itemsPerCycle + 0.1f)
                            {
                                hit.TargetItemsPerMinute -= itemsPerCycle;
                                hit.UpdatePredecessors();
                            }
                            Invalidate();
                            return;
                        }
                    }
                }

                if (e.Button == MouseButtons.Left)
                {
                    SaveUndo();
                    _draggedNode = hit;
                }
            }
            else
            {
                _isPanning = true;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            PointF wNow = ScreenToWorld(e.Location);
            PointF wLast = ScreenToWorld(_lastMousePos);
            float dx = wNow.X - wLast.X;
            float dy = wNow.Y - wLast.Y;

            if (_isPanning) { _viewOffset.X += (e.X - _lastMousePos.X); _viewOffset.Y += (e.Y - _lastMousePos.Y); }
            else if (_draggedNode != null) { _draggedNode.Location = new Point((int)(_draggedNode.Location.X + dx), (int)(_draggedNode.Location.Y + dy)); }
            else if (_draggedLabel.HasValue)
            {
                var n = _nodes.First(x => x.Id == _draggedLabel.Value.nodeId);
                if (!n.LabelOffsets.ContainsKey(_draggedLabel.Value.prevId)) n.LabelOffsets[_draggedLabel.Value.prevId] = new Point(0, 0);
                var o = n.LabelOffsets[_draggedLabel.Value.prevId];
                n.LabelOffsets[_draggedLabel.Value.prevId] = new Point((int)(o.X + dx), (int)(o.Y + dy));
            }
            _lastMousePos = e.Location;
            Invalidate();
        }

        private void OnMouseUp(object sender, MouseEventArgs e) { _isPanning = false; _draggedNode = null; _draggedLabel = null; }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            float factor = e.Delta > 0 ? 1.1f : 0.9f;
            float newZoom = Math.Max(0.1f, Math.Min(_zoom * factor, 3.0f));

            if (newZoom != _zoom)
            {
                PointF worldMousePos = ScreenToWorld(e.Location);

                _zoom = newZoom;
                _viewOffset.X = e.X - (worldMousePos.X * _zoom);
                _viewOffset.Y = e.Y - (worldMousePos.Y * _zoom);

                Invalidate();
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z && _undoStack.Count > 0)
            {
                _redoStack.Push(_nodes.Select(n => n.Location).ToList());
                var state = _undoStack.Pop();
                for (int i = 0; i < Math.Min(state.Count, _nodes.Count); i++) _nodes[i].Location = state[i];
            }
            else if (e.Control && e.KeyCode == Keys.Y && _redoStack.Count > 0)
            {
                _undoStack.Push(_nodes.Select(n => n.Location).ToList());
                var state = _redoStack.Pop();
                for (int i = 0; i < Math.Min(state.Count, _nodes.Count); i++) _nodes[i].Location = state[i];
            }
            Invalidate();
        }

        private Point GetLabelPos(ProductionNode n, ProductionNode prev)
        {
            int idx = n.Recipe.Inputs.FindIndex(x => x.Name == prev.Recipe.ItemName);
            Point p1 = new Point(prev.Location.X + prev.Size.Width, prev.Location.Y + prev.Size.Height / 2);
            Point p2 = new Point(n.Location.X, n.Location.Y + 55 + (idx * 25));
            Point mid = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
            if (n.LabelOffsets.ContainsKey(prev.Id)) { mid.X += n.LabelOffsets[prev.Id].X; mid.Y += n.LabelOffsets[prev.Id].Y; }
            return mid;
        }
        
        private void OnPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = InterpolationMode.Low;

            e.Graphics.TranslateTransform(_viewOffset.X, _viewOffset.Y);
            e.Graphics.ScaleTransform(_zoom, _zoom);

            DrawGrid(e.Graphics);
            foreach (var n in _nodes) DrawNodeConnections(e.Graphics, n);
            foreach (var n in _nodes) DrawNode(e.Graphics, n);

            e.Graphics.ResetTransform();
            DrawControlsOverlay(e.Graphics);
        }

        private void DrawControlsOverlay(Graphics g)
        {
            string controlsText =
                "• Canvas Panning: Hold Left-Click on background or Middle-Mouse\n" +
                "• Move Nodes: Hold Left-Click on node\n" +
                "• Zoom: Mouse Wheel\n" +
                "• Create: Use button at the top\n" +
                "• Undo/Redo: Ctrl + Z / Ctrl + Y";

            Font controlsFont = new Font("Segoe UI", 12f);
            Color textColor = Color.FromArgb(150, 200, 200, 200);

            Size textSize = TextRenderer.MeasureText(controlsText, controlsFont);
            Point drawPos = new Point(this.ClientSize.Width - textSize.Width - 20, 20);

            using (SolidBrush brush = new SolidBrush(textColor))
            {
                g.DrawString(controlsText, controlsFont, brush, drawPos);
            }
        }

        private void DrawGrid(Graphics g)
        {
            int s = 100;
            using Pen p = new Pen(Color.FromArgb(30, 30, 35));
            float wL = -_viewOffset.X / _zoom, wT = -_viewOffset.Y / _zoom;
            float wR = wL + Width / _zoom, wB = wT + Height / _zoom;
            for (float x = wL - (wL % s); x < wR; x += s) g.DrawLine(p, x, wT, x, wB);
            for (float y = wT - (wT % s); y < wB; y += s) g.DrawLine(p, wL, y, wR, y);
        }

        private void DrawNode(Graphics g, ProductionNode n)
        {
            Rectangle b = new Rectangle(n.Location, n.Size);
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(45, 45, 55))) g.FillRectangle(bg, b);

            bool isRoot = !_nodes.Any(o => o.InputNodes.Contains(n));
            g.DrawRectangle(isRoot ? Pens.Cyan : Pens.DimGray, b);

            Image? icon = GetIcon(n.Recipe.ItemName);
            if (icon != null) g.DrawImage(icon, b.X + 10, b.Y + 10, 24, 24);
            g.DrawString($"{n.Recipe.OutputAmount}x {n.Recipe.ItemName}", _fontBold, Brushes.White, b.X + 40, b.Y + 14);

            if (isRoot && !n.Recipe.IsRawResource)
            {
                string rateText = $"{n.TargetItemsPerMinute:0.#}/min";
                Size sz = TextRenderer.MeasureText(rateText, _fontBold);
                g.DrawString(rateText, _fontBold, Brushes.LightGreen, b.Right - sz.Width - 10, b.Y + 14);

                string totalPowerText = "Total Power Use: " + FormatPowerValue(n.GetTotalTreePower()) + " (Without Mining Rigs)";
                g.DrawString(totalPowerText, _fontBold, Brushes.Yellow, b.X, b.Y - 20);
            }

            using (Pen separatorPen = new Pen(Color.FromArgb(80, 80, 90), 1))
            {
                g.DrawLine(separatorPen, b.X + 10, b.Y + 42, b.Right - 10, b.Y + 42);
            }

            for (int i = 0; i < n.Recipe.Inputs.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(n.Recipe.Inputs[i].Name)) continue;

                int y = b.Y + 55 + (i * 30);
                Rectangle ingRect = new Rectangle(b.X + 8, y - 4, b.Width - 16, 26);
                using (SolidBrush ingBg = new SolidBrush(Color.FromArgb(50, 65, 85))) g.FillRectangle(ingBg, ingRect);

                Image? ii = GetIcon(n.Recipe.Inputs[i].Name);
                if (ii != null) g.DrawImage(ii, b.X + 12, y, 18, 18);
                g.DrawString($"{n.Recipe.Inputs[i].Amount}x {n.Recipe.Inputs[i].Name}", _fontSmall, Brushes.Silver, b.X + 36, y + 1);
            }

            using (Pen separatorPen = new Pen(Color.FromArgb(80, 80, 90), 1))
            {
                g.DrawLine(separatorPen, b.X + 10, b.Bottom - 38, b.Right - 10, b.Bottom - 38);
            }

            if (n.Recipe.IsRawResource)
            {
                g.DrawString($"Total Demand: {n.TargetItemsPerMinute:0.#}/min", _fontBold, Brushes.Cyan, b.X + 10, b.Bottom - 28);
            }
            else
            {
                float exact = n.GetExactMachines();
                g.DrawString($"({exact:0.##}x)", _fontSmall, Brushes.Gray, b.X + 10, b.Bottom - 28);

                Image? mIcon = GetIcon(n.Recipe.MachineName);
                int textXOffset = 55;
                if (mIcon != null)
                {
                    g.DrawImage(mIcon, b.X + 55, b.Bottom - 31, 22, 22);
                    textXOffset = 82;
                }
                g.DrawString($"{(int)Math.Ceiling(exact)}x {n.Recipe.MachineName}", _fontBold, Brushes.Orange, b.X + textXOffset, b.Bottom - 28);

                string powerNodeText = "Power Use: " + FormatPowerValue(n.GetNodePower());
                Size pSize = TextRenderer.MeasureText(powerNodeText, _fontSmallBold);
                g.DrawString(powerNodeText, _fontSmallBold, Brushes.LightSkyBlue, b.Right - pSize.Width - 10, b.Bottom - 28);

                if (isRoot)
                {
                    Rectangle btnPlus = new Rectangle(b.Right + 5, b.Y, 30, 30);
                    Rectangle btnMinus = new Rectangle(b.Right + 5, b.Y + 35, 30, 30);

                    g.FillRectangle(Brushes.ForestGreen, btnPlus);
                    g.FillRectangle(Brushes.Firebrick, btnMinus);
                    g.DrawString("+", _fontBold, Brushes.White, btnPlus.X + 7, btnPlus.Y + 5);
                    g.DrawString("-", _fontBold, Brushes.White, btnMinus.X + 9, btnMinus.Y + 5);
                }
            }
        }

        private string FormatPowerValue(float value)
        {
            if (value >= 1000)
            {
                return (value / 1000f).ToString("0.##") + "K";
            }
            return value.ToString("0.##");
        }

        private void DrawNodeConnections(Graphics g, ProductionNode n)
        {
            foreach (var prev in n.InputNodes)
            {
                int idx = n.Recipe.Inputs.FindIndex(x => x.Name == prev.Recipe.ItemName);
                if (idx == -1) continue;

                Point p1 = new Point(prev.Location.X + prev.Size.Width, prev.Location.Y + prev.Size.Height / 2);

                int targetY = n.Location.Y + 59 + (idx * 30);
                Point p2 = new Point(n.Location.X, targetY);

                using (Pen p = new Pen(Color.FromArgb(200, 200, 100), 2))
                {
                    g.DrawBezier(p, p1, new Point(p1.X + 60, p1.Y), new Point(p2.X - 60, p2.Y), p2);
                }

                Point lp = GetLabelPos(n, prev);
                string txt = $"{prev.TargetItemsPerMinute:0.#} /min";

                Size sz = TextRenderer.MeasureText(txt, _fontSmallBold);
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(20, 20, 25)))
                {
                    g.FillRectangle(bgBrush, lp.X - sz.Width / 2, lp.Y - sz.Height / 2, sz.Width, sz.Height);
                }
                g.DrawString(txt, _fontSmallBold, Brushes.Yellow, lp.X - sz.Width / 2, lp.Y - sz.Height / 2);
            }
        }
    }
}