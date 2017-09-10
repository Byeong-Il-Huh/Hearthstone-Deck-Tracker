﻿using System;
using System.Threading.Tasks;
using Hearthstone_Deck_Tracker.HsReplay;
using Hearthstone_Deck_Tracker.Live.Data;
using Hearthstone_Deck_Tracker.Utility.Extensions;
using Hearthstone_Deck_Tracker.Utility.Logging;
using Hearthstone_Deck_Tracker.Utility.Twitch;

namespace Hearthstone_Deck_Tracker.Live
{
	internal class LiveDataManager
	{
		private static BoardStateWatcher _boardStateWatcher;
		private static BoardStateWatcher BoardStateWatcher => _boardStateWatcher ?? (_boardStateWatcher = GetBoardStateWatcher());

		public static event Action<bool> OnStreamingChecked;

		private static BoardStateWatcher GetBoardStateWatcher()
		{
			var boardStateWatcher = new BoardStateWatcher();
			boardStateWatcher.OnNewBoardState += OnNewBoardState;
			return boardStateWatcher;
		}

		public static async void WatchBoardState()
		{
			if(_running)
				return;
			if(!Config.Instance.SendLiveUpdates || Config.Instance.SelectedTwitchUser <= 0)
				return;
			var streaming = await TwitchApi.IsStreaming(Config.Instance.SelectedTwitchUser);
			OnStreamingChecked?.Invoke(streaming);
			if(!streaming)
				return;
			_running = true;
			BoardStateWatcher.Start();
			//PayloadDump.Clear();
		}

		public static void Stop()
		{
			if(!_running)
				return;
			BoardStateWatcher.Stop();
			SendUpdate(PayloadFactory.GameEnd());
			_running = false;
			//var json = JsonConvert.SerializeObject(PayloadDump, Formatting.Indented);
			//using(var wr = new StreamWriter("D:/hdt-payload-dump.json"))
			//	wr.Write(json);
		}

		private static DateTime _lastSent = DateTime.MinValue;
		private static int _currentHash;
		private static bool _running;

		private static async void SendUpdate(Payload payload)
		{
			var hash = payload.GetHashCode();
			_currentHash = hash;
			await Task.Delay(Math.Max(0, 1000 - (int)(DateTime.Now - _lastSent).TotalMilliseconds));
			if(_currentHash == hash)
			{
				//PayloadDump.Add(payload);
				_lastSent = DateTime.Now;
				Log.Debug($"Sending payload {hash} (type={payload.Type})");
				HSReplayNetOAuth.SendTwitchPayload(payload).Forget();
			}
			else
			{
				Log.Debug($"Skipped payload {hash} (type={payload.Type})");
			}
		}

		private static void OnNewBoardState(BoardState boardState)
		{
			SendUpdate(PayloadFactory.BoardState(boardState));
		}

		//public List<Payload> PayloadDump { get; set; } = new List<Payload>();
	}
}
