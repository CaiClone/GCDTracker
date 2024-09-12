using ImGuiNET;

namespace GCDTracker.UI {
    public interface IWindow {
        string WindowName { get; }
        bool IsMoveable { get; }
        public bool ShouldDraw(bool inCombat, bool noUI);
        public void DrawWindow(PluginUI ui, bool inCombat, bool noUI) {
            if (ShouldDraw(inCombat, noUI)) {
                ui.SetupWindow(WindowName, IsMoveable);
                Draw(ui);
                ImGui.End();
            }
        }
        public void Draw(PluginUI ui);
    }
}