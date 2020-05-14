using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using vip.popop.pcr.GHelper.Utilities;

namespace vip.popop.pcr.GHelper.Modules {
    class LinkGenerator {

        private static readonly List<Regex> Commands = new List<Regex> {
            new Regex(@"\A憨批链接"),
            new Regex(@"\A生成\s*\!\!(.*)\!\!(.*)\!\!(.*)\!\!(.*)"),
            new Regex(@"\A生成\s*\!\!(.*)\!\!(.*)\!\!(.*)"),
        };

        public void OnInitialize() {
            Event_GroupMessage.OnGroupMessage += OnMessage;
            Event_PrivateMessage.OnPrivateMessage += OnMessage;
        }

        public void OnMessage(object sender, CQEventEventArgs e) {

            Match m;
            string msg = null;

            if (e.GetType() == typeof(CQGroupMessageEventArgs)) {
                msg = ((CQGroupMessageEventArgs)e).Message.Text;
            } else {
                msg = ((CQPrivateMessageEventArgs)e).Message.Text;
            }

            if ((m = Commands[0].Match(msg)).Success) {
                Ai.Reply(e, " 欢迎使用憨批链接生成器，输入格式\"生成!!{url}!!{title}!!{content}(optional:!!{image_url})\"");
            } else if ((m = Commands[1].Match(msg)).Success) {
                Ai.Reply(e, CQApi.CQCode_ShareLink(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value).ToSendString());
            } else if ((m = Commands[2].Match(msg)).Success) {
                Ai.Reply(e, CQApi.CQCode_ShareLink(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value).ToSendString());
            }

        }
    }
}
