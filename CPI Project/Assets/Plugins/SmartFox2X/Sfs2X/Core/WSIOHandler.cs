using System;
using System.Text;
using Sfs2X.Bitswarm;
using Sfs2X.Entities.Data;
using Sfs2X.Exceptions;
using Sfs2X.Logging;
using Sfs2X.Protocol;
using Sfs2X.Protocol.Serialization;
using Sfs2X.Util;

namespace Sfs2X.Core
{
	public class WSIOHandler : IoHandler
	{
		public static readonly int SHORT_BYTE_SIZE = 2;

		public static readonly int INT_BYTE_SIZE = 4;

		private ISocketClient socketClient;

		private Logger log;

		private IProtocolCodec protocolCodec;

		public IProtocolCodec Codec => protocolCodec;

		public WSIOHandler(ISocketClient socketClient)
		{
			this.socketClient = socketClient;
			log = socketClient.Log;
			protocolCodec = new WSProtocolCodec(this, socketClient);
		}

		public void OnDataWrite(IMessage message)
		{
			if (socketClient.IsBinProtocol)
			{
				ByteArray byteArray = message.Content.ToBinary();
				bool compressed = byteArray.Length > socketClient.CompressionThreshold;
				CheckSize(byteArray.Bytes);
				int num = SHORT_BYTE_SIZE;
				if (byteArray.Length > 65535)
				{
					num = INT_BYTE_SIZE;
				}
				PacketHeader header = new PacketHeader(encrypted: false, compressed, blueBoxed: false, num == INT_BYTE_SIZE);
				if (socketClient.Debug)
				{
					log.Info("Data written: " + message.Content.GetHexDump());
				}
				WriteBinaryData(header, byteArray);
			}
			else
			{
				string text = message.Content.ToJson();
				byte[] bytes = Encoding.UTF8.GetBytes(text);
				CheckSize(bytes);
				if (socketClient.Debug)
				{
					log.Info("Data written [" + bytes.Length + " bytes]: " + text);
				}
				socketClient.Socket.Write(text);
			}
		}

		private void CheckSize(byte[] binData)
		{
			if (binData.Length > socketClient.MaxMessageSize)
			{
				throw new SFSCodecError("Message size is too big: " + binData.Length + ", the server limit is: " + socketClient.MaxMessageSize);
			}
		}

		private void WriteBinaryData(PacketHeader header, ByteArray binData)
		{
			ByteArray byteArray = new ByteArray();
			if (header.Compressed)
			{
				binData.Compress();
			}
			byteArray.WriteByte(header.Encode());
			if (header.BigSized)
			{
				byteArray.WriteInt(binData.Length);
			}
			else
			{
				byteArray.WriteUShort(Convert.ToUInt16(binData.Length));
			}
			byteArray.WriteBytes(binData.Bytes);
			socketClient.Socket.Write(byteArray.Bytes);
		}

		public void OnDataRead(string jsonData)
		{
			if (jsonData.Length == 0)
			{
				throw new SFSError("Unexpected empty string data: no readable informations available!");
			}
			if (socketClient.Debug)
			{
				log.Info("Data read: " + jsonData);
			}
			ISFSObject packet = SFSObject.NewFromJsonData(jsonData);
			protocolCodec.OnPacketRead(packet);
		}

		public void OnDataRead(ByteArray data)
		{
			log.Debug("Handling new data of size " + data.Length);
			byte b = data.ReadByte();
			if (~(b & 0x80) > 0)
			{
				throw new SFSError("Unexpected header byte: " + b + "\n" + DefaultObjectDumpFormatter.HexDump(data));
			}
			PacketHeader packetHeader = PacketHeader.FromBinary(b);
			data = ResizeByteArray(data, 1, data.Length - 1);
			log.Debug("Handling header size. Length: " + data.Length + " (" + (packetHeader.BigSized ? "big" : "small") + ")");
			int num = SHORT_BYTE_SIZE;
			if (packetHeader.BigSized)
			{
				num = INT_BYTE_SIZE;
			}
			data = ResizeByteArray(data, num, data.Length - num);
			if (packetHeader.Compressed)
			{
				data.Uncompress();
			}
			protocolCodec.OnPacketRead(data);
		}

		private ByteArray ResizeByteArray(ByteArray array, int pos, int len)
		{
			byte[] array2 = new byte[len];
			Buffer.BlockCopy(array.Bytes, pos, array2, 0, len);
			return new ByteArray(array2);
		}
	}
}
