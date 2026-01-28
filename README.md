# Arknights: Endfield Simple Modeler

A lightweight **Windows Forms tool** for tracking and managing production lines in *Arknights: Endfield*.  
Built quickly to help visualize big production setups and keep things organized.

## Disclaimer
- This is an early version: not all items or icons are implemented.  
- Bugs or missing features may appear.  
- Created in just a few hours for personal use, shared here in case it helps someone else.  
- The workspace **does not save**, since only one production line is shown at a time.  
- I don’t know when or if I will update this, but at least it exists

## Features
- Track **one production line at a time**  

## How to use
1. **Unzip** the downloaded release ZIP.  
2. Run the program by starting **`EndfieldModeler.exe`**.  
3. **Left-click and drag** to move around the workspace.  
4. Use the **controls in the top-right corner** of the app.  
5. To create a new production line, press the **“Create new Production-Line”** button.  
6. Nodes are **moveable** and display:  
   - Production item  
   - Production need  
   - Machine required and how many  
   - **Power usage**  
7. Node connections show **parts per minute**.  
8. On the production line **END node**, use **+ / -** to increase/decrease the amount.  
9. For ores, the **total demand per minute** is displayed.  
10. On the **END node**, the **total power usage** of the production line is shown *(excluding mining rigs)*.  
11. **Undo/Redo node positions:**  
    - `Ctrl + Z` → undo node movement  
    - `Ctrl + Y` → redo node movement  
12. **Zoom:**  
    - **Scroll the mouse wheel** to zoom in/out of the workspace

## Screenshots
### HC-Battery Production Line
![HC-Battery](screenshots/hc_valley_battery.png)

### Dense Ferrium Powder Production Line
![Dense Ferrium Powder](screenshots/dense_ferrium_powder.png)
