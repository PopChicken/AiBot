using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using vip.popop.pcr.GHelper.Utilities;

namespace vip.popop.pcr.GHelper.Modules {
    class Solo {

        class Challenge {

            public bool bigPin;

            public long toId;

            public long timeLeft;

        }

        Dictionary<long, Dictionary<long, Challenge>> Challenges = new Dictionary<long, Dictionary<long, Challenge>>();

        Dictionary<long, Dictionary<long, HashSet<long>>> Requests = new Dictionary<long, Dictionary<long, HashSet<long>>>();

        public int validSpan { get; set; }

        private static readonly List<Regex> Commands = new List<Regex> {
            new Regex(@"\A拼点\z"),
            new Regex(@"\[(.*)\]\s*拼点\s*\z"),
            new Regex(@"\[(.*)\]\s*不拼\s*\z"),
            new Regex(@"\A\s*弃拼\s*\z"),
            new Regex(@"\A\s*查战书\s*\z"),
            new Regex(@"\[(.*)\]\s*大拼点\s*\z"),
        };

        public void OnInitialize() {
            Event_GroupMessage.OnGroupMessage += OnGroupMessage;
        }

        public void OnGroupMessage(object sender, CQGroupMessageEventArgs e) {

            string msg = e.Message;
            long fromId = e.FromQQ.Id;
            long groupId = e.FromGroup.Id;

            if (!Challenges.ContainsKey(groupId)) {
                Challenges.Add(groupId, new Dictionary<long, Challenge>());
            }
            if (!Requests.ContainsKey(groupId)) {
                Requests.Add(groupId, new Dictionary<long, HashSet<long>>());
            }

            Match m;
            bool bigPin = false;

            if ((m = Commands[0].Match(msg)).Success) {
                Ai.Reply(e, e.FromQQ.CQCode_At(), "欢迎来玩拼点~", Environment.NewLine,
                    "“@对方 拼点”可以下达战书或者应战", Environment.NewLine,
                    "“@对方 不拼”可以拒绝对方的挑战", Environment.NewLine,
                    "“弃拼”可以放弃挑战", Environment.NewLine,
                    "“查战书”可以查看有谁向你发起了挑战", Environment.NewLine,
                    Environment.NewLine,
                    "“@对方 大拼点”可以向对方发起赌上尊严的挑战或者应战！", Environment.NewLine,
                    "【注意】已经有等待对方接受的战书后，就不能再发战书了哦~", Environment.NewLine,
                    "【注意】大拼点会占用拼点的战书槽，且要用“@对方 大拼点”进行应战");
            } else if (((bigPin = (m = Commands[5].Match(msg)).Success) || (m = Commands[1].Match(msg)).Success)
                  && e.Message.CQCodes.FindAll(c => c.Function == CQFunction.At).Count == 1) {

                long toId = long.Parse(e.Message.CQCodes.FindAll(c => c.Function == CQFunction.At)[0].Items["qq"]);

                if (toId == e.FromQQ.Id) { //查自雷
                    Ai.Reply(e, e.FromQQ.CQCode_At(), " 不可以自雷的哦");
                } else {
                    if (!Requests[groupId].ContainsKey(fromId)) {
                        Requests[groupId].Add(fromId, new HashSet<long>());
                    }

                    if (Requests[groupId][fromId].Contains(toId)) {
                        if (bigPin == Challenges[groupId][toId].bigPin) {
                            CQCode attackerAt = new CQCode(CQFunction.At, new KeyValuePair<string, string>("qq", toId.ToString()));
                            CQCode defenderAt = e.FromQQ.CQCode_At();
                            CQCode winnerAt, loserAt;

                            long? loserId;

                            string roundMsg = "";

                            roundMsg += attackerAt + " 与 " + defenderAt + (Challenges[groupId][toId].bigPin ? " 堵上尊严" : " ") + "的对决开始了！" + Environment.NewLine;

                            Random rad = new Random();
                            int attack = rad.Next(1, 7);
                            int defend = rad.Next(1, 7);

                            roundMsg += "挑战者" + attackerAt + $" 掷出了{attack}点！" + Environment.NewLine +
                                "应战者" + defenderAt + $" 掷出了{defend}点！" + Environment.NewLine;

                            if (attack > defend) {
                                roundMsg += "挑战者" + attackerAt + $" 击败了" + "应战者" + defenderAt;
                                winnerAt = attackerAt;
                                loserAt = defenderAt;
                                loserId = fromId;
                            } else if (attack == defend) {
                                roundMsg += "挑战者" + attackerAt + $" 与" + "应战者" + defenderAt + " 和局~" + Environment.NewLine + "以和为贵哦~";
                                winnerAt = null;
                                loserAt = null;
                                loserId = null;
                            } else {
                                roundMsg += "应战者" + defenderAt + $" 击败了" + "挑战者" + attackerAt;
                                winnerAt = defenderAt;
                                loserAt = attackerAt;
                                loserId = toId;
                            }

                            if (Challenges[groupId][toId].bigPin) {
                                if (loserId == null) roundMsg += Environment.NewLine + "和局~没有人会被惩罚w";
                                else roundMsg += Environment.NewLine + loserAt + " 在大拼点中被击败了！接受处罚吧！";
                            }

                            Ai.Reply(e, roundMsg);

                            if (Challenges[groupId][toId].bigPin && loserId != null) {
                                e.FromGroup.SetGroupMemberBanSpeak(loserId ?? 0, TimeSpan.FromMinutes(Math.Abs(attack - defend)));
                            }

                            Challenges[groupId].Remove(toId);
                            Requests[groupId][fromId].Remove(toId);

                        } else {
                            Ai.Reply(e, $"挑战类型是{(Challenges[groupId][toId].bigPin ? "大拼点" : "拼点")}", Environment.NewLine, "请使用对应指令哦~");
                        }

                    } else {
                        if (Challenges[groupId].ContainsKey(fromId)) {
                            Ai.Reply(e, e.FromQQ.CQCode_At(), $" 你已经向 {Challenges[groupId][fromId].toId} 拼点了，发送 弃拼 可以放弃哦~");
                        } else {
                            Challenges[groupId].Add(fromId, new Challenge {
                                bigPin = bigPin,
                                toId = toId,
                                timeLeft = 300000
                            });
                            if (!Requests[groupId].ContainsKey(toId)) {
                                Requests[groupId].Add(toId, new HashSet<long>());
                            }
                            Requests[groupId][toId].Add(fromId);
                            Ai.Reply(e, e.FromQQ.CQCode_At(), $" 成功下达战书，请等待对方回应√");
                            new Native.Sdk.Cqp.Model.QQ(e.CQApi, toId).SendPrivateMessage($" {e.FromGroup.GetGroupInfo().Name} 中有人向你挑战，快去看看吧~");
                        }
                    }
                }
            } else if ((m = Commands[2].Match(msg)).Success && e.Message.CQCodes.FindAll(c => c.Function == CQFunction.At).Count == 1) {
                long toId = long.Parse(e.Message.CQCodes.FindAll(c => c.Function == CQFunction.At)[0].Items["qq"]);

                if (!Challenges[groupId].ContainsKey(toId)) {
                    Ai.Reply(e, e.FromQQ.CQCode_At(), " 对方并没有挑战你哦~");
                } else {
                    Challenges[groupId].Remove(toId);
                    Requests[groupId][fromId].Remove(toId);
                    Ai.Reply(e, e.FromQQ.CQCode_At(), " 你拒绝了对方的挑战！");
                }

            } else if ((m = Commands[3].Match(msg)).Success) {
                if (Challenges[groupId].ContainsKey(fromId)) {
                    Ai.Reply(e, e.FromQQ.CQCode_At(), $" 已放弃拼点");
                    Requests[groupId][fromId].Remove(e.FromQQ.Id);
                    Challenges[groupId].Remove(fromId);
                } else {
                    Ai.Reply(e, e.FromQQ.CQCode_At(), $" 没有可放弃的拼点哦~");
                }
            } else if ((m = Commands[4].Match(msg)).Success) {
                if (!Requests[groupId].ContainsKey(fromId) || Requests[groupId][fromId].Count == 0) {
                    Ai.Reply(e, e.FromQQ.CQCode_At(), $" 你没有等待回应的战书哦~");
                } else {
                    string reply = "";
                    foreach (long req in Requests[groupId][fromId]) {
                        reply += req.ToString() + " ";
                    }
                    Ai.Reply(e, " 目前向你挑战的人有: ", reply);
                }
            }
        }
    }
}
