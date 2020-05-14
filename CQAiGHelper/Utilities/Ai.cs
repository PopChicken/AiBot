using Native.Sdk.Cqp.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vip.popop.pcr.GHelper.Utilities {
    class Ai {
        public static void Reply(CQEventEventArgs e, params object[] message) {
            if (e.GetType() == typeof(CQGroupMessageEventArgs)) {
                ((CQGroupMessageEventArgs)e).FromGroup.SendGroupMessage(message);
            } else {
                ((CQPrivateMessageEventArgs)e).FromQQ.SendPrivateMessage(message);
            }
        }
    }
}
