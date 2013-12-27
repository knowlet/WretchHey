using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace WretchHey {
	public class Program {
		public static void Main (string[] args) {
			List<WretchAlbum> albums = new List<WretchAlbum>();
			WebClient wc = new WebClient();
			Regex regCovers = new Regex(@"http://\w+\.wretch\.yimg\.com/(\w+)/(\d+)/thumbs/t(\d+)\.jpg");
			string album_html_content, account;

			if (args.Length < 1) {
				Console.WriteLine("[-] Usage: WretchHey [Wretch ID]");
				return;
			}

			account = args[0];

			try {
				byte[] buff = wc.DownloadData("http://webcache.googleusercontent.com/search?q=cache:www.wretch.cc/album/" + account);
				album_html_content = Encoding.UTF8.GetString(buff);
			}
			catch (WebException) {
				Console.WriteLine(string.Format("[-] Can't get '{0}' :(", account));
				return;
			}
			if (regCovers.IsMatch(album_html_content)) {
				foreach (Match item in regCovers.Matches(album_html_content)) {
					// string account = item.Groups[1].Value;
					string aid     = item.Groups[2].Value;
					string pid     = item.Groups[3].Value;
					albums.Add(new WretchAlbum(account, aid, pid));
				}
				string base_path = Path.Combine(Environment.CurrentDirectory, account);
				if (!Directory.Exists(base_path))
					Directory.CreateDirectory(base_path);
				foreach (WretchAlbum item in albums) {
					string path = Path.Combine(base_path, item.ID);
					string url = item.Cover();
					string name = item.CoverName();
					if (!Directory.Exists(path))
						Directory.CreateDirectory(path);
					Console.WriteLine(string.Format("[+] Save to: {0}", path));
					Console.WriteLine(string.Format("[+] Downloading: {0}", url));
					try {
						wc.DownloadFile(url, Path.Combine(path, name));
					}
					catch (WebException) {
						File.Delete(Path.Combine(path, name));
					}
					int d = 1; // 1 for prev, 2 for next
					while (true) {
						url = (d == 1) ? item.Prev() : item.Next();
						name = (d == 1) ? item.PrevName() : item.NextName();
						try {
							Console.WriteLine(string.Format("[+] Downloading: {0}", url));
							wc.DownloadFile(url, Path.Combine(path, name));
						}
						catch (WebException) {
							File.Delete(Path.Combine(path, name));
							if (d == 1)
								d = 2;
							else
								break;
						}
					}
				}
			} else {
				Console.WriteLine("[*] No albums can read :(");
			}
		}
	}

	public class WretchAlbum {
		string account, id;
		int prev, next, pid;
		// Album id
		public string ID {
			get {
				return this.id;
			}
		}

		public WretchAlbum (string Account, string ID, string PID) {
			this.Init(Account, ID, int.Parse(PID));
		}

		public WretchAlbum (string Account, string ID, int PID) {
			this.Init(Account, ID, PID);
		}

		private void Init (string Account, string ID, int PID) {
			this.account = Account;
			this.id = ID;
			this.prev = this.next = this.pid = PID;
		}

		// Building file URL
		private string mkurl (int id) {
			return string.Format("http://119.160.255.175/{0}/{1}/{2:D10}.jpg", this.account, this.id, id);
		}

		// Building file name
		private string mkname (int id) {
			return string.Format("{0:D10}.jpg", id);
		}

		// Return cover's URL
		public string Cover() {
			return mkurl(this.pid);
		}
		
		// Return cover's file name
		public string CoverName() {
			return mkname(this.pid);
		}

		// Return previous file URL
		public string Prev() {
			return mkurl(--this.prev);
		}

		// Return previous file name
		public string PrevName() {
			return mkname(this.prev);
		}

		// Return next file URL
		public string Next() {
			return mkurl(++this.next);
		}

		// Return next file name
		public string NextName() {
			return mkname(this.next);
		}
	}
}
