using Native.Sdk.Cqp.Enum;
using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using vip.popop.pcr.GHelper.Utilities;

namespace vip.popop.pcr.GHelper.Modules {
    class Solo {

        class Challenge {

            public bool bigPin;

            public long toId;

            public long timeLeft;

            public int stack;

        }

        Dictionary<long, Dictionary<long, Challenge>> Challenges = new Dictionary<long, Dictionary<long, Challenge>>();

        Dictionary<long, Dictionary<long, HashSet<long>>> Requests = new Dictionary<long, Dictionary<long, HashSet<long>>>();

        Dictionary<long, HashSet<long>> Prison = new Dictionary<long, HashSet<long>>();

        public int validSpan { get; set; }

        public int checkTime { get; set; }

        public int maxStack { get; set; }

        private static readonly List<Regex> Commands = new List<Regex> {
            new Regex(@"\A拼点\z"),
            new Regex(@"\[(.*)\]\s*拼点\s*\z"),
            new Regex(@"\[(.*)\]\s*不拼\s*\z"),
            new Regex(@"\A\s*弃拼\s*\z"),
            new Regex(@"\A\s*查战书\s*\z"),
            new Regex(@"\[(.*)\]\s*(\d+倍)*大拼点*\s*\z"),
            new Regex(@"\A都给爷解\z")
        };

        public void OnInitialize() {
            Event_GroupMessage.OnGroupMessage += OnGroupMessage;
            Timer t = new Timer(checkTime);
            t.Elapsed += new ElapsedEventHandler((_s, _e) => RequestTimeOut(_s, _e));
            t.AutoReset = true;
            t.Enabled = true;
        }

        public void RequestTimeOut(object o, ElapsedEventArgs e) {
            foreach (long g in Requests.Keys) {
                foreach (HashSet<long> r in Requests[g].Values) {
                    foreach (long id in r) {
                        if (Challenges[g][id].timeLeft <= checkTime) {
                            r.Remove(id);
                            Challenges[g].Remove(id);
                        }
                        Challenges[g][id].timeLeft -= checkTime;
                    }
                }
            }
        }

        private void MakePairing(long group, long from, Challenge ch) {
            Challenges[group].Add(from, ch);
            Requests[group][ch.toId].Add(from);
        }

        private void RemovePairing(long group, long from, long to) {
            Challenges[group].Remove(from);
            Requests[group][to].Remove(from);
        }

        public void OnGroupMessage(object sender, CQGroupMessageEventArgs e) {
            try {

                if (!Prison.ContainsKey(e.FromGroup.Id)) {
                    Prison.Add(e.FromGroup.Id, new HashSet<long>());
                } else if (Prison[e.FromGroup.Id].Contains(e.FromQQ.Id)) Prison[e.FromGroup.Id].Remove(e.FromQQ.Id);

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

                        bool haveStack = m.Groups.Count == 3 && m.Groups[2].Value.Trim(' ').Length > 0;
                        int wantedStack = 1;
                        if (bigPin && haveStack) int.TryParse(m.Groups[2].Value.Substring(0, m.Groups[2].Value.Length - 1), out wantedStack);

                        if (!Requests[groupId].ContainsKey(fromId)) {
                            Requests[groupId].Add(fromId, new HashSet<long>());
                        }

                        if (Requests[groupId][fromId].Contains(toId)) {

                            int originStack = Challenges[groupId][toId].stack;
                            bool matched = false;
                            if (bigPin == Challenges[groupId][toId].bigPin) {
                                if (bigPin && wantedStack == originStack) {
                                    matched = true;
                                } else if (!bigPin) matched = true;
                            }

                            if (matched) {
                                CQCode attackerAt = new CQCode(CQFunction.At, new KeyValuePair<string, string>("qq", toId.ToString()));
                                CQCode defenderAt = e.FromQQ.CQCode_At();
                                CQCode winnerAt, loserAt;

                                long? loserId;

                                string roundMsg = "";

                                roundMsg += attackerAt + " 与 " + defenderAt + (Challenges[groupId][toId].bigPin ? " 赌上尊严" : " ") + "的对决开始了！" + Environment.NewLine;

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
                                    e.FromGroup.SetGroupMemberBanSpeak(loserId ?? 0, TimeSpan.FromMinutes(Math.Abs(attack - defend) * originStack));
                                    Prison[groupId].Add((long)loserId);
                                }

                                RemovePairing(groupId, toId, fromId);

                            } else {
                                string type = "拼点";
                                if (Challenges[groupId][toId].bigPin) {
                                    if (Challenges[groupId][toId].stack > 1) type = $"{Challenges[groupId][toId].stack}倍大拼点";
                                    else type = "大拼点";
                                }
                                Ai.Reply(e, $"挑战类型是{type}", Environment.NewLine, "请使用对应指令哦~");
                            }

                        } else {
                            if (Challenges[groupId].ContainsKey(fromId)) {
                                Ai.Reply(e, e.FromQQ.CQCode_At(), $" 你已经向 {e.FromGroup.GetGroupMemberInfo(Challenges[groupId][fromId].toId).Nick} 拼点了，发送 弃拼 可以放弃哦~");
                            } else if (bigPin && haveStack && (wantedStack <= 1 || wantedStack > maxStack)) {
                                Ai.Reply(e, e.FromQQ.CQCode_At(), $" 倍率只可以是{2}到{maxStack}范围内的整数哦~");
                            } else {
                                if (!Requests[groupId].ContainsKey(toId)) {
                                    Requests[groupId].Add(toId, new HashSet<long>());
                                }
                                MakePairing(groupId, fromId, new Challenge {
                                    bigPin = bigPin,
                                    toId = toId,
                                    timeLeft = validSpan,
                                    stack = wantedStack
                                });
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
                        string reply = Environment.NewLine + "挑战者  类型  有效时间" + Environment.NewLine;
                        foreach (long req in Requests[groupId][fromId]) {
                            reply += e.FromGroup.GetGroupMemberInfo(req).Nick + " " +
                                (Challenges[groupId][req].bigPin ? $"{(Challenges[groupId][req].stack == 1 ? "" : $"{Challenges[groupId][req].stack}倍")}大拼点" : "拼点") + " "
                                + Math.Ceiling(Challenges[groupId][req].timeLeft / 60000.0) + "分钟" + Environment.NewLine;
                        }
                        Ai.Reply(e, " 目前你收到的战书有: ", reply);
                    }
                } else if (Commands[6].Match(msg).Success && e.FromGroup.GetGroupMemberInfo(e.FromQQ).MemberType == QQGroupMemberType.Creator) {
                    foreach (long mem in Prison[groupId]) {
                        e.FromGroup.RemoveGroupMemberBanSpeak(mem);
                    }
                    Prison.Clear();
                    Ai.Reply(e, e.FromQQ.CQCode_At(), " 已全部解除禁言w");
                }
            } catch (KeyNotFoundException) { }
        }
    }
}
