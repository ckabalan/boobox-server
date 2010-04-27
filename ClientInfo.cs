﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BooBox;

namespace BooBoxServer {
	public class ClientInfo {
		public Socket Socket;
		public int Index;
		public String Name;
		public String Version;
		public ConnectionStatus ConnectionStatus;
		public String DataBuffer;

		/// <summary>
		/// Public constructor for createing a Client object
		/// </summary>
		/// <param name="iSocket">Socket associated with the Client object</param>
		/// <param name="iClientInfoIndex">Index number of the associated Client object</param>
		public ClientInfo(Socket iSocket, int iClientInfoIndex) {
			Socket = iSocket;
			Index = iClientInfoIndex;
			ConnectionStatus = ConnectionStatus.Pending;
			OnClientConnect();
		}

		/// <summary>
		/// Triggered when the Client and Server have achieved a full TCP connection
		/// </summary>
		private void OnClientConnect() {
			Send(Protocol.CreateHELLO(Config.Instance.ServerName));
		}

		/// <summary>
		/// Sends data to associated Client
		/// </summary>
		/// <param name="Data">String containing the data to be sent to the client</param>
		public void Send(String Data) {
			if (Data.Length > 150) {
				Console.WriteLine("To ClientInfo " + Index + ":\t" + Data.Substring(0, 150) + "...");
			} else {
				Console.WriteLine("To ClientInfo " + Index + ":\t" + Data);
			}
			byte[] byteData = Encoding.UTF8.GetBytes(Data + "\x4");
			Socket.Send(byteData);
		}

		/// <summary>
		/// Triggered when data is recieved from the Client
		/// </summary>
		/// <param name="Data">Contains data sent from the Client</param>
		public void OnReceiveData(String Data) {
			DataBuffer = "";
			if (Data.Length > 1000) {
				Console.WriteLine("From ClientInfo " + Index + ":\t" + Data.Substring(0, 1000) + "...");
			} else {
				Console.WriteLine("From ClientInfo " + Index + ":\t" + Data);
			}
			ParseMessage(Data);
		}

		public void ParseMessage(String Data) {
			char[] spaceDelim = new char[] { ' ' };
			String[] tokenData = Data.Split(spaceDelim, 2);
			switch (tokenData[0]) {
				case "HELLOR":
					#region HELLOR
					Name = tokenData[1];
					Log.AddClientText("Connected to \"" + Name + "\".", Index);
					Send(Protocol.CreateVERSION());
					break;
					#endregion
				case "VERSIONR":
					#region VERSIONR
					Version = tokenData[1];
					Log.AddClientText("\"" + Name + "\" running at version " + Version, Index);
					Send(Protocol.CreateDETAILS(GenerateDetailList()));
					if (Config.Instance.PasswordRequired == false) {
						ConnectionStatus = ConnectionStatus.Connected;
						Send(Protocol.CreateOK());
					}
					break;
					#endregion
				case "PASS":
					#region PASS
					if (Config.Instance.PasswordRequired == true) {
						if (tokenData[1] == Config.Instance.ServerPassword) {
							Log.AddClientText("Accepted password from \"" + Name + "\"", Index);
							Send("PASSR PASS_ACCEPTED");
							ConnectionStatus = ConnectionStatus.Connected;
							Send(Protocol.CreateOK());
							Log.AddClientText("Fully connected to \"" + Name + "\"!", Index);
						} else {
							Send("PASSR PASS_REJECTED");
							Log.AddClientText("Rejected password from \"" + Name + "\"", Index);
						}
					} else {
						Send("PASSR PASS_NOT_REQUIRED");
						Log.AddClientText("\"" + Name + "\" sent password, but it was not required. Ingoring.", Index);
					}
					break;
					#endregion
				case "GOODBYE":
					#region GOODBYE
					Log.AddClientText("\"" + Name + "\" sent GOODBYE. Disconnecting...", Index);
					Socket.Close();
					break;
					#endregion
			}
		}

		private List<ServerDetail> GenerateDetailList() {
			List<ServerDetail> tempDetailList = new List<ServerDetail>();
			ServerDetail tempServerDetail = new ServerDetail();
			tempServerDetail.Name = "GUID"; tempServerDetail.Value = Config.Instance.GUID; tempDetailList.Add(tempServerDetail);
			tempServerDetail.Name = "STREAMPORT"; tempServerDetail.Value = Config.Instance.CommStreamPort.ToString(); tempDetailList.Add(tempServerDetail);
			tempServerDetail.Name = "PASS";
			if (Config.Instance.PasswordRequired == true) { tempServerDetail.Value = "1"; } else { tempServerDetail.Value = "0"; }
			tempDetailList.Add(tempServerDetail);
			return tempDetailList;
		}


	}
}
