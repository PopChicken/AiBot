using Native.Sdk.Cqp.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Native.Sdk.Cqp.Model;
using Native.Sdk.Cqp;
using System.IO;
using System.Timers;

namespace vip.popop.pcr.GHelper.Modules {
    class Repeater {

        public class Info {

            public bool isCoolingDown { get; set; }

            public string lastMsg { get; set; }

        }

        public double repeatChance;

        public int cooldown;

        public List<string> banWords;

        Dictionary<long, Info> groupList = new Dictionary<long, Info>();

        public void OnInitialize() {
            Event_GroupMessage.OnGroupMessage += OnGroupMessage;
            Event_AppEnable.OnAppEnable += OnLoad;
        }

        public void OnLoad(object obj, CQAppEnableEventArgs e) {
            List<GroupInfo> groups = e.CQApi.GetGroupList().ToList();
            foreach (GroupInfo group in groups) {
                if (groupList.ContainsKey(group.Group.Id)) continue;
                groupList.Add(group.Group.Id, new Info { isCoolingDown = false, lastMsg = "" });
            }
        }

        public void OnGroupMessage(object sender, CQGroupMessageEventArgs e) {
            long groupId = e.FromGroup.Id;
            if (!groupList.ContainsKey(groupId)) groupList.Add(groupId, new Info { isCoolingDown = false, lastMsg = "" });
            string msg = e.Message;
            if (groupList[groupId].isCoolingDown) return;

            

            if (msg.CompareTo(groupList[groupId].lastMsg) == 0) {
                int rad = new Random().Next(1, 100);
                if (rad <= 100 * repeatChance) {
                    e.FromGroup.SendGroupMessage(e.Message.ToSendString());
                    groupList[groupId].isCoolingDown = true;
                    Timer t = new Timer(cooldown);
                    t.Elapsed += new ElapsedEventHandler((_s, _e) => RepeatCoolDown(_s, _e, groupId));
                    t.AutoReset = false;
                    t.Enabled = true;
                }
            }
            groupList[groupId].lastMsg = msg;
        }

        private void RepeatCoolDown(object source, ElapsedEventArgs e, long Id) {
            groupList[Id].isCoolingDown = false;
        }
    }
}
