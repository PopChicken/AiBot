using Native.Sdk.Cqp.EventArgs;
using Native.Sdk.Cqp.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using vip.popop.pcr.GHelper.Utilities;
using F23.StringSimilarity;

namespace vip.popop.pcr.GHelper.Modules {
    class Phonograph {

        class Music {

            public enum Status {
                E404A,
                E404B,
                Success
            }

            public Status Stat;

            public string StatText;

            public ClientType Type;

            public string Id;

            public string Name;

            public List<string> Singers;

            public ClientType Client;

        }

        class ExtractRes {
            public string res;
            public bool extracted;
        }

        public enum ClientType { Auto, QQ, Netease };

        public ClientType Client { get; set; }

        public bool IsWithLink { get; set; }

        private static List<Regex> Commands = new List<Regex> {
            new Regex(@"\A点歌\z"),
            new Regex(@"\A点歌\s*(.*)by:(.*)"),
            new Regex(@"\A点歌\s*(.*)"),
        };

        public void OnInitialize() {
            Event_GroupMessage.OnGroupMessage += OnMessage;
            Event_PrivateMessage.OnPrivateMessage += OnMessage;
        }

        public void OnMessage(object sender, CQEventEventArgs e) {
            CQCode atCode = null;
            Match m;
            string msg;

            if (e.GetType() == typeof(CQGroupMessageEventArgs)) {
                atCode = ((CQGroupMessageEventArgs)e).FromQQ.CQCode_At();
                msg = ((CQGroupMessageEventArgs)e).Message.Text;
            } else {
                msg = ((CQPrivateMessageEventArgs)e).Message.Text;
            }

            if ((m = Commands[0].Match(msg.Trim(' '))).Success) {
                Ai.Reply(e, $" 想让爱梅斯帮你搜什么呀？请用 点歌+歌名 或者 点歌+歌名+By:+作者 呼叫我哦~");
            } else if ((m = Commands[1].Match(msg.ToLower())).Success) {
                if (m.Groups[1].Value.Trim(' ').Length == 0) {
                    Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), " 没告诉我歌名我怎么搜索呀");
                } else {
                    Music music = FindBetter(Client, m.Groups[1].Value.Trim(' '), m.Groups[2].Value.Trim(' '));
                    ClientType client = music.Client;
                    if (music.Stat == Music.Status.E404A) {
                        if (music.StatText != null) Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), $" 找不到这个名字的歌曲，你是不是想搜索{ m.Groups[2].Value.Trim(' ') }呢？");
                        else Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), " 搜索不到哦..."); ;
                    } else if (music.Stat == Music.Status.E404B) {
                        Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), $" 爱梅斯没有找到由这个作者演唱的歌曲哦");
                    } else {
                        Ai.Reply(e, $" [CQ:music,type={(client == ClientType.Netease ? "163" : "qq")},id={music.Id}]");
                        if (IsWithLink && client == ClientType.Netease) Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), $" https://music.163.com/#/song?id={music.Id}");
                        else if (IsWithLink && client == ClientType.QQ) Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), $" https://y.qq.com/n/yqq/song/{music.Id}_num.html");
                    }
                }
            } else if ((m = Commands[2].Match(msg)).Success) {
                if (m.Groups[1].Value.Trim(' ').Length == 0) {
                    Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), " 没告诉我歌名我怎么搜索呀");
                } else {
                    Music music = FindBetter(Client, m.Groups[1].Value.Trim(' '));
                    ClientType client = music.Client;
                    if (music.Stat == Music.Status.E404A) {
                        if (m.Groups[2].Value.Trim(' ').Length > 0) Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), $" 找不到这个名字的歌曲，你是不是想搜索{ m.Groups[2].Value.Trim(' ') }呢？");
                        else Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), " 搜索不到哦...");
                    } else if (music.Stat == Music.Status.E404B) {
                        Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), $" 爱梅斯找不到符合要求的歌曲呢...");
                    } else {
                        Ai.Reply(e, $" [CQ:music,type={(client == ClientType.Netease ? "163" : "qq")},id={music.Id}]");
                        if (IsWithLink && client == ClientType.Netease) Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), $" https://music.163.com/#/song?id={music.Id}");
                        else if (IsWithLink && client == ClientType.QQ) Ai.Reply(e, atCode == null ? "" : atCode.ToSendString(), $" https://y.qq.com/n/yqq/song/{music.Id}_num.html");
                    }
                }
            }
        }

        private ExtractRes AutoExtract(string s, string fac) {
            if (fac.Length == 1) return s.IndexOf(fac[0]) == -1 ?
                new ExtractRes {
                    res = s,
                    extracted = false
                } :
                new ExtractRes {
                    res = fac,
                    extracted = true
                };
            Match m;
            if ((m = new Regex($"{fac[0]}(.*){fac[fac.Length - 1]}").Match(s)).Success) {
                return new ExtractRes {
                    res = $"{fac[0]}{m.Groups[1].Value ?? ""}{fac[fac.Length - 1]}",
                    extracted = true
                };
            }
            return new ExtractRes {
                res = s,
                extracted = false
            };
        }

        private Music FindBetter(ClientType client, string s, string author = null) {
            if (client == ClientType.Auto) {
                Music qq = SearchMusic(ClientType.QQ, s + author, author);
                Music ne = SearchMusic(ClientType.Netease, s + author, author);
                if (qq.Stat == Music.Status.Success && ne.Stat == Music.Status.Success) {
                    string name_qq = qq.Name, name_ne = ne.Name;

                    ExtractRes ene = AutoExtract(name_ne, s);
                    ExtractRes eqq = AutoExtract(name_qq, s);

                    if (!ene.extracted && !eqq.extracted) return ne;
                    name_ne = ene.res;
                    name_qq = eqq.res;

                    Levenshtein lev = new Levenshtein();
                    double dis_qq = lev.Distance(name_qq, s);
                    double dis_ne = lev.Distance(name_ne, s);

                    //e.FromGroup.SendGroupMessage($" QQ结果“{name_qq}” Levenshtein距离: {dis_qq}， 163结果 “{name_ne}” Levenshtein距离: {dis_ne}"
                    //    + Environment.NewLine + $"自动选择: {(dis_qq < dis_ne ? "QQ音乐" : "网易云音乐")}");
                    if (dis_qq < dis_ne) return qq;
                    else return ne;
                } else if (qq.Stat == Music.Status.Success) return qq;
                else return ne;
            } else {
                return SearchMusic(client, s, author);
            }
        }

        private Music SearchMusic(ClientType client, string s, string author = null) {
            if (client == ClientType.QQ) {
                HttpWebRequest request = WebRequest.CreateHttp($"https://c.y.qq.com/soso/fcgi-bin/client_search_cp?w={s}");
                request.Method = "GET";
                request.ContentType = "application/json";
                HttpWebResponse response;
                try {
                    response = (HttpWebResponse)request.GetResponse();
                } catch (Exception) {
                    return new Music {
                        Stat = Music.Status.E404A,
                        StatText = null,
                        Type = client,
                        Id = null,
                        Name = null,
                        Singers = null,
                        Client = ClientType.QQ
                    };
                }

                StreamReader jsonReader = new StreamReader(response.GetResponseStream());
                string rawjson = jsonReader.ReadToEnd();
                jsonReader.Close();
                rawjson = rawjson.Remove(0, 9);
                rawjson = rawjson.Remove(rawjson.Length - 1);

                QQ.QQResp resp = JsonConvert.DeserializeObject<QQ.QQResp>(rawjson);

                if (resp.Message.CompareTo("no results") == 0 || resp.Message.CompareTo("query forbid") == 0) {
                    return new Music {
                        Stat = Music.Status.E404A,
                        StatText = null,
                        Type = client,
                        Id = null,
                        Name = null,
                        Singers = null,
                        Client = ClientType.QQ
                    };
                }

                if (author == null) {
                    return new Music {
                        Stat = Music.Status.Success,
                        StatText = null,
                        Type = client,
                        Id = resp.Data.Song.List[0].SongId,
                        Name = resp.Data.Song.List[0].SongName,
                        Singers = resp.Data.Song.List[0].GetSingers(),
                        Client = ClientType.QQ
                    };
                }

                Regex reg = new Regex(author.ToLower());
                foreach (QQ.SongResp song in resp.Data.Song.List) {
                    foreach (QQ.SingerResp singer in song.Singer) {
                        if (reg.Match(singer.Name.ToLower()).Success) {
                            return new Music {
                                Stat = Music.Status.Success,
                                StatText = null,
                                Type = client,
                                Id = song.SongId,
                                Name = song.SongName,
                                Singers = song.GetSingers(),
                                Client = ClientType.QQ
                            };
                        }
                    }
                }
                return new Music {
                    Stat = Music.Status.E404B,
                    StatText = null,
                    Type = client,
                    Id = null,
                    Name = null,
                    Singers = null,
                    Client = ClientType.QQ
                };

            } else if (client == ClientType.Netease) {
                HttpWebRequest request = WebRequest.CreateHttp($"https://v1.alapi.cn/api/music/search?keyword={s}");
                request.Method = "GET";
                request.ContentType = "application/json";
                HttpWebResponse response;
                try {
                    response = (HttpWebResponse)request.GetResponse();
                } catch (Exception) {
                    return new Music {
                        Stat = Music.Status.E404A,
                        StatText = null,
                        Type = client,
                        Id = null,
                        Name = null,
                        Singers = null,
                        Client = ClientType.Netease
                    };
                }

                StreamReader jsonReader = new StreamReader(response.GetResponseStream());
                string rawjson = jsonReader.ReadToEnd();
                jsonReader.Close();
                NE.NeteaseResp resp = JsonConvert.DeserializeObject<NE.NeteaseResp>(rawjson);

                if (resp.Data.SongCount == 0) {
                    return new Music {
                        Stat = Music.Status.E404A,
                        StatText = resp.Data.QueryCorrected == null ? null : resp.Data.QueryCorrected[0],
                        Type = client,
                        Id = null,
                        Name = null,
                        Singers = null,
                        Client = ClientType.Netease
                    };
                }

                if (author == null) {
                    return new Music {
                        Stat = Music.Status.Success,
                        StatText = null,
                        Type = client,
                        Id = resp.Data.Songs[0].Id,
                        Name = resp.Data.Songs[0].Name,
                        Singers = resp.Data.Songs[0].GetArtists(),
                        Client = ClientType.Netease
                    };
                }

                Regex reg = new Regex(author.ToLower());
                foreach (NE.SongResp song in resp.Data.Songs) {
                    foreach (NE.ArtistResp artist in song.Artists) {
                        if (reg.Match(artist.Name.ToLower()).Success) {
                            return new Music {
                                Stat = Music.Status.Success,
                                StatText = null,
                                Type = client,
                                Id = song.Id,
                                Name = song.Name,
                                Singers = song.GetArtists(),
                                Client = ClientType.Netease
                            };
                        }
                    }
                }
                return new Music {
                    Stat = Music.Status.E404B,
                    StatText = null,
                    Type = client,
                    Id = null,
                    Name = null,
                    Singers = null,
                    Client = ClientType.Netease
                };
            }
            return null;
        }

    }

    class NE {
        public class NeteaseResp {
            public int Code { get; set; }
            public string Msg { get; set; }
            public DataResp Data { get; set; }

        }

        public class DataResp {
            public List<SongResp> Songs { get; set; }

            public List<string> QueryCorrected { get; set; }

            public int SongCount { get; set; }
        }

        public class SongResp {
            public string Id { get; set; }
            public string Name { get; set; }

            public List<ArtistResp> Artists { get; set; }

            public List<string> GetArtists() {
                List<string> artists = new List<string>();
                foreach (ArtistResp artist in Artists) {
                    artists.Add(artist.Name);
                }
                return artists;
            }
        }

        public class ArtistResp {
            public string Name { get; set; }
        }
    }

    class QQ {
        public class QQResp {
            public DataResp Data { get; set; }

            public string Message { get; set; }
        }

        public class DataResp {
            public SongsResp Song { get; set; }
        }

        public class SongsResp {
            public List<SongResp> List { get; set; }
        }

        public class SongResp {
            public string SongId { get; set; }

            public string SongName { get; set; }

            public List<SingerResp> Singer { get; set; }

            public List<string> GetSingers() {
                List<string> singers = new List<string>();
                foreach (SingerResp singer in Singer) {
                    singers.Add(singer.Name);
                }
                return singers;
            }
        }

        public class SingerResp {
            public string Name { get; set; }
        }
    }
}
