using EndfieldModeler.Models;
using EndfieldModeler.Nodes;
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

        public Form1()
        {
            DoubleBuffered = true;
            Size = new Size(1400, 900);
            BackColor = Color.FromArgb(20, 20, 25);
            Text = "Simple Endfield Modeler - Pre Alpha";
            KeyPreview = true;

            InitializeRecipes();
            InitializeSearchMenu();

            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
            Paint += OnPaint;
            KeyDown += OnKeyDown;
        }

        private void InitializeRecipes()
        {
            void AddRecipe(string name, string machine, float timeInSeconds, float outputAmount, params (string, float)[] ins) =>
                _recipes.Add(new Recipe { ItemName = name, MachineName = machine, CraftingTimeSeconds = timeInSeconds, OutputAmount = outputAmount, Inputs = ins.Select(x => new Ingredient { Name = x.Item1, Amount = x.Item2 }).ToList() });
            void AddRaw(string name) => _recipes.Add(new Recipe { ItemName = name, MachineName = "Welt", IsRawResource = true });

            AddRaw("Originium Ore");
            AddRaw("Amethyst Ore");
            AddRaw("Ferrium Ore");

            AddRecipe("Amethyst Part", "Fitting Unit", 2, 1, ("Amethyst Fiber", 1));
            AddRecipe("Amethyst Fiber", "Refining Unit", 2, 1, ("Amethyst Ore", 1));
            AddRecipe("Tal HC Battery", "Packaging Unit", 10, 1, ("Steel Part", 10), ("Dense Originium Powder", 15));
            AddRecipe("Steel Part", "Fitting Unit", 2, 1, ("Steel", 1));
            AddRecipe("Steel", "Refining Unit", 2, 1, ("Dense Ferrium Powder", 1));
            AddRecipe("Dense Ferrium Powder", "Grinding Unit", 2, 1, ("Sandleaf Powder", 1), ("Ferrium Powder", 2));
            AddRecipe("Ferrium Powder", "Shredding Unit", 2, 1, ("Ferrium", 1));
            AddRecipe("Ferrium", "Refining Unit", 2, 1, ("Ferrium Ore", 1));
            AddRecipe("Dense Originium Powder", "Grinding Unit", 2, 1, ("Sandleaf Powder", 1), ("Originium Powder", 2));
            AddRecipe("Originium Powder", "Shredding Unit", 2, 1, ("Originium Ore", 1));
            AddRecipe("Sandleaf Powder", "Shredding Unit", 2, 3, ("Sandleaf", 1));
            AddRecipe("Sandleaf", "Planting Unit", 2, 1, ("Sandleaf Seed", 1));
            AddRecipe("Buckflower", "Planting Unit", 2, 1, ("Buckflower Seed", 1));
            AddRecipe("Buckflower Seed", "Seed-Picking Unit", 2, 2, ("Buckflower", 1));
            AddRecipe("Sandleaf Seed", "Seed-Picking Unit", 2, 2, ("Sandleaf", 1));
            AddRecipe("Cryston Component", "Gearing Unit", 10, 1, ("Packed Origocrust", 10), ("Cryston Fiber", 10));
            AddRecipe("Packed Origocrust", "Refining Unit", 2, 1, ("Dense Origocrust Powder", 1));
            AddRecipe("Dense Origocrust Powder", "Refining Unit", 2, 1, ("Dense Originium Powder", 1));
            AddRecipe("Cryston Fiber", "Refining Unit", 2, 1, ("Cryston Powder", 1));
            AddRecipe("Cryston Powder", "Grinding Unit", 2, 1, ("Sandleaf Powder", 1), ("Amethyst Powder", 2));
            AddRecipe("Amethyst Powder", "Shredding Unit", 2, 1, ("Amethyst Fiber", 1));
            AddRecipe("Carbon", "Refining Unit", 2, 1, ("Buckflower", 1));
            AddRecipe("Carbon", "Refining Unit", 2, 1, ("Sandleaf", 1));
            AddRecipe("Origocrust", "Refining Unit", 2, 1, ("Originium Ore", 1));
            AddRecipe("Stabilized Carbon", "Refining Unit", 2, 1, ("Dense Carbon Powder", 1));
            AddRecipe("Carbon Powder", "Refining Unit", 2, 2, ("Sandleaf Powder", 3));
            AddRecipe("Carbon Powder", "Shredding Unit", 2, 2, ("Carbon", 1));
            AddRecipe("Origocrust Powder", "Shredding Unit", 2, 1, ("Origocrust", 1));
            AddRecipe("Aketine Powder", "Shredding Unit", 2, 2, ("Aketine", 1));
            AddRecipe("Dense Carbon Powder", "Grinding Unit", 2, 1, ("Sandleaf Powder", 1), ("Carbon Powder", 2));
            AddRecipe("Amethyst Bottle", "Moulding Unit", 2, 1, ("Amethyst Fiber", 2));
            AddRecipe("Ferrium Bottle", "Moulding Unit", 2, 1, ("Ferrium", 2));
            AddRecipe("Cryston Bottle", "Moulding Unit", 2, 1, ("Cryston Fiber", 2));
            AddRecipe("Steel Bottle", "Moulding Unit", 2, 1, ("Steel", 2));
            AddRecipe("Ferrium Part", "Fitting Unit", 2, 1, ("Ferrium", 1));
            AddRecipe("Amethyst Component", "Gearing Unit", 10, 1, ("Amethyst Fiber", 5), ("Origocrust", 5));
            AddRecipe("Ferrium Component", "Gearing Unit", 10, 1, ("Ferrium", 10), ("Origocrust", 10));
            AddRecipe("Cryston Part", "Fitting Unit", 2, 1, ("Cryston Fiber", 1));
            AddRecipe("Tal LC Battery", "Packaging Unit", 10, 1, ("Originium Powder", 10), ("Amethyst Part", 5));
            AddRecipe("Tal SC Battery", "Packaging Unit", 10, 1, ("Originium Powder", 15), ("Ferrium Part", 10));
        }

        private void InitializeSearchMenu()
        {
            _searchPanel = new Panel { Size = new Size(280, 450), BackColor = Color.FromArgb(35, 35, 40), Visible = false, BorderStyle = BorderStyle.FixedSingle };
            _searchBox = new TextBox { Dock = DockStyle.Top, BackColor = Color.FromArgb(30, 30, 35), ForeColor = Color.White, Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle };
            _searchBox.TextChanged += (s, e) => UpdateSearchList(_searchBox.Text);
            _searchList = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(5) };
            _searchPanel.Controls.Add(_searchList);
            _searchPanel.Controls.Add(_searchBox);
            Controls.Add(_searchPanel);
        }

        private void UpdateSearchList(string filter)
        {
            _searchList.SuspendLayout();
            _searchList.Controls.Clear();
            var matches = _recipes
                .Where(r => string.IsNullOrWhiteSpace(filter) || r.ItemName.ToLower().Contains(filter.ToLower()))
                .OrderBy(r => r.ItemName);

            foreach (var r in matches)
            {
                Panel itemPanel = new Panel { Width = 250, Height = 40, Margin = new Padding(0, 0, 0, 2), BackColor = Color.FromArgb(45, 45, 50) };

                PictureBox itemIcon = new PictureBox { Image = GetIcon(r.ItemName), Size = new Size(24, 24), Location = new Point(5, 8), SizeMode = PictureBoxSizeMode.Zoom };
                PictureBox machIcon = new PictureBox { Image = GetIcon(r.MachineName), Size = new Size(16, 16), Location = new Point(32, 12), SizeMode = PictureBoxSizeMode.Zoom };

                Button b = new Button
                {
                    Text = r.ItemName,
                    Size = new Size(120, 40),
                    Location = new Point(55, 0),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                b.FlatAppearance.BorderSize = 0;
                b.Click += (s, e) => { CreateFullTree(r); _searchPanel.Visible = false; };

                itemPanel.Controls.Add(itemIcon);
                itemPanel.Controls.Add(machIcon);
                itemPanel.Controls.Add(b);

                int startX = 180;
                foreach (var ing in r.Inputs)
                {
                    PictureBox ingIcon = new PictureBox { Image = GetIcon(ing.Name), Size = new Size(16, 16), Location = new Point(startX, 12), SizeMode = PictureBoxSizeMode.Zoom };
                    itemPanel.Controls.Add(ingIcon);
                    startX += 20;
                }
                _searchList.Controls.Add(itemPanel);
            }
            _searchList.ResumeLayout();
        }

        private Image? GetIcon(string name)
        {
            if (_iconCache.TryGetValue(name, out var img)) return img;

            string cleanName = name.Replace("-", "_").Replace(" ", "_").ToLower() + ".png";
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] paths = {
                Path.Combine(baseDir, "Assets", "AIC Products", cleanName),
                Path.Combine(baseDir, "Assets", "Naturals", cleanName),
                Path.Combine(baseDir, "Assets", "Machines", cleanName)
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
            var root = new ProductionNode { Recipe = rootRecipe, Location = new Point(Width / 2 + 200, Height / 2), TargetItemsPerMinute = baseRate };
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

                var node = new ProductionNode
                {
                    Recipe = rec,
                    Location = targetLoc,
                    TargetItemsPerMinute = needed
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
                    Rectangle newRect = new Rectangle(safeLoc, new Size(300, 200));

                    if (existingRect.IntersectsWith(newRect))
                    {
                        safeLoc.Y += 220;
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

            if (e.Button == MouseButtons.Right)
            {
                _searchPanel.Location = e.Location;
                _searchPanel.Visible = true;
                _searchBox.Text = "";
                UpdateSearchList("");
                _searchBox.Focus();
                return;
            }

            _searchPanel.Visible = false;

            var hit = _nodes.LastOrDefault(n => new Rectangle(n.Location, n.Size).Contains((int)worldPos.X, (int)worldPos.Y));
            if (hit != null)
            {
                bool isRoot = !_nodes.Any(o => o.InputNodes.Contains(hit));
                if (isRoot)
                {
                    int relX = (int)worldPos.X - hit.Location.X;
                    int relY = (int)worldPos.Y - hit.Location.Y;

                    if (relY > hit.Size.Height - 40)
                    {
                        float itemsPerCycle = (60f / hit.Recipe.CraftingTimeSeconds) * hit.Recipe.OutputAmount;

                        if (relX > hit.Size.Width - 40)
                        {
                            hit.TargetItemsPerMinute += itemsPerCycle;
                            hit.UpdatePredecessors();
                            Invalidate();
                            return;
                        }
                        else if (relX > hit.Size.Width - 80 && relX < hit.Size.Width - 40)
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
                SaveUndo();
                _draggedNode = hit;
            }
            else { _isPanning = true; }
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
            _zoom = Math.Max(0.1f, Math.Min(_zoom * factor, 3.0f));
            Invalidate();
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

            if (n.Recipe.IsRawResource)
            {
                g.DrawString($"Total Demand: {n.TargetItemsPerMinute:0.#}/min", _fontBold, Brushes.Cyan, b.X + 10, b.Bottom - 25);
            }
            else
            {
                float exact = n.GetExactMachines();

                g.DrawString($"({exact:0.##}x)", _fontSmall, Brushes.Gray, b.X + 10, b.Bottom - 25);

                Image? mIcon = GetIcon(n.Recipe.MachineName);
                if (mIcon != null)
                {
                    g.DrawImage(mIcon, b.X + 55, b.Bottom - 28, 20, 20);
                    g.DrawString($"{(int)Math.Ceiling(exact)}x {n.Recipe.MachineName}", _fontBold, Brushes.Orange, b.X + 80, b.Bottom - 25);
                }
                else
                {
                    g.DrawString($"{(int)Math.Ceiling(exact)}x {n.Recipe.MachineName}", _fontBold, Brushes.Orange, b.X + 60, b.Bottom - 25);
                }

                if (isRoot)
                {
                    g.DrawString($"{n.TargetItemsPerMinute:0.#} /min", _fontBold, Brushes.LightGreen, b.Right - 90, b.Y + 35);

                    Rectangle btnMinus = new Rectangle(b.Right - 75, b.Bottom - 35, 30, 25);
                    Rectangle btnPlus = new Rectangle(b.Right - 40, b.Bottom - 35, 30, 25);

                    g.FillRectangle(Brushes.Firebrick, btnMinus);
                    g.FillRectangle(Brushes.ForestGreen, btnPlus);

                    g.DrawString("-", _fontBold, Brushes.White, btnMinus.X + 10, btnMinus.Y + 4);
                    g.DrawString("+", _fontBold, Brushes.White, btnPlus.X + 8, btnPlus.Y + 4);
                }
            }

            for (int i = 0; i < n.Recipe.Inputs.Count; i++)
            {
                int y = b.Y + 55 + (i * 25);
                Image? ii = GetIcon(n.Recipe.Inputs[i].Name);
                if (ii != null) g.DrawImage(ii, b.X + 10, y, 16, 16);
                g.DrawString($"{n.Recipe.Inputs[i].Amount}x {n.Recipe.Inputs[i].Name}", _fontSmall, Brushes.Silver, b.X + 32, y);
            }
        }

        private void DrawNodeConnections(Graphics g, ProductionNode n)
        {
            foreach (var prev in n.InputNodes)
            {
                int idx = n.Recipe.Inputs.FindIndex(x => x.Name == prev.Recipe.ItemName);
                Point p1 = new Point(prev.Location.X + prev.Size.Width, prev.Location.Y + prev.Size.Height / 2);
                Point p2 = new Point(n.Location.X, n.Location.Y + 55 + (idx * 25));

                using (Pen p = new Pen(Color.FromArgb(200, 200, 100), 2))
                    g.DrawBezier(p, p1, new Point(p1.X + 60, p1.Y), new Point(p2.X - 60, p2.Y), p2);

                Point lp = GetLabelPos(n, prev);
                string txt = $"{prev.TargetItemsPerMinute:0.#} /min";

                Size sz = TextRenderer.MeasureText(txt, _fontSmallBold);
                g.FillRectangle(new SolidBrush(Color.FromArgb(20, 20, 25)), lp.X - sz.Width / 2, lp.Y - sz.Height / 2, sz.Width, sz.Height);
                g.DrawString(txt, _fontSmallBold, Brushes.Yellow, lp.X - sz.Width / 2, lp.Y - sz.Height / 2);
            }
        }
    }
}