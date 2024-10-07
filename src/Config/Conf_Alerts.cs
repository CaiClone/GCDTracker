using System.Collections.Generic;
using System.Numerics;
using GCDTracker.Config.Alerts;
using ImGuiNET;

namespace GCDTracker.Config {
    public partial class Configuration {
        public List<ConfAlert> ClipAlerts = new();
        public List<ConfAlert> ABCAlerts = new();
        public List<ConfAlert> QueueLockAlerts = new();
        public List<ConfAlert> SlideCastAlerts = new();

        private void DrawAlertConfig() {
            if (ImGui.CollapsingHeader("Clip Alerts", ImGuiTreeNodeFlags.DefaultOpen)) {
                DrawAlertSectionConfig(ClipAlerts, SectionType.Clip);
            }
            if (ImGui.CollapsingHeader("A-B-C Alerts", ImGuiTreeNodeFlags.DefaultOpen)) {
                DrawAlertSectionConfig(ABCAlerts, SectionType.ABC);
            }
            if (ImGui.CollapsingHeader("QueueLock Alerts", ImGuiTreeNodeFlags.DefaultOpen)) {
                DrawAlertSectionConfig(QueueLockAlerts, SectionType.QueueLock);
            }
            if (ImGui.CollapsingHeader("SlideCast Alerts", ImGuiTreeNodeFlags.DefaultOpen)) {
                DrawAlertSectionConfig(SlideCastAlerts, SectionType.SlideCast);
            }
        }

        private void DrawAlertSectionConfig(List<ConfAlert> alerts, SectionType section) {
            for (int i = 0; i < alerts.Count; i++) {
                ConfAlert alert = alerts[i];
                ImGui.PushID(i);
                if (ImGui.TreeNodeEx(alert.Name, ImGuiTreeNodeFlags.DefaultOpen)) {
                    alert.DrawConfig();
                    if (ImGui.Button("Remove Alert")) {
                        alerts.RemoveAt(i);
                        i--;
                        ImGui.TreePop();
                        ImGui.PopID();
                        continue;
                    }
                    ImGui.TreePop();
                }
                ImGui.PopID();
            }

            if (ImGui.Button($"Add Alert##{section}")) {
                ImGui.OpenPopup($"AddAlertPopup##{section}");
            }

            if (ImGui.BeginPopup($"AddAlertPopup##{section}")) {
                if (ImGui.MenuItem("Popup")) {
                    alerts.Add(new CA_PopUp {Section = section });
                    ImGui.CloseCurrentPopup();
                }
                if (ImGui.MenuItem("Pulse")) {
                    alerts.Add(new CA_Pulse {Section = section });
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }
    }
}
