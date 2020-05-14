using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Sdk.Cqp;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Interface;
using Native.Sdk.Cqp.Model;
using System.Text.RegularExpressions;
using System.Collections;
using System.Timers;

namespace vip.popop.pcr.GHelper {

    public class Event_CQStartUp : ICQStartup {

        public delegate void EventsHandler(object sender, CQStartupEventArgs e);

        public static event EventsHandler OnStartUp;

        public void CQStartup(object obj, CQStartupEventArgs e) {
            //Main.Initialize();
            OnStartUp?.Invoke(obj, e);
        }
    }
    public class Event_AppEnable : IAppEnable {

        public delegate void EventsHandler(object sender, CQAppEnableEventArgs e);

        public static event EventsHandler OnAppEnable;

        public void AppEnable(object sender, CQAppEnableEventArgs e) {
            Main.Initialize();
            OnAppEnable?.Invoke(sender, e);
        }
    }
    public class Event_PrivateMessage : IPrivateMessage {

        public delegate void EventsHandler(object sender, CQPrivateMessageEventArgs e);

        public static event EventsHandler OnPrivateMessage;

        public static void ClearHandler() {
            if (OnPrivateMessage == null) return;
            Delegate[] dels = OnPrivateMessage.GetInvocationList();
            foreach (Delegate @delegate in dels) {
                OnPrivateMessage -= @delegate as EventsHandler;
            }
        }

        public void PrivateMessage(object sender, CQPrivateMessageEventArgs e) {
            OnPrivateMessage?.Invoke(sender, e);
        }
    }
    public class Event_GroupMessage : IGroupMessage {

        public delegate void EventsHandler(object sender, CQGroupMessageEventArgs e);

        public static event EventsHandler OnGroupMessage;

        public static void ClearHandler() {
            if (OnGroupMessage == null) return;
            Delegate[] dels = OnGroupMessage.GetInvocationList();
            foreach (Delegate @delegate in dels) {
                OnGroupMessage -= @delegate as EventsHandler;
            }
        }

        /// <summary>
        /// 收到群消息
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件参数</param>
        public void GroupMessage(object sender, CQGroupMessageEventArgs e) {

            OnGroupMessage?.Invoke(sender, e);

            /*
            LastMsg = msg;*/
            // 设置该属性, 表示阻塞本条消息, 该属性会在方法结束后传递给酷Q
            e.Handler = false;
        }
    }
}
