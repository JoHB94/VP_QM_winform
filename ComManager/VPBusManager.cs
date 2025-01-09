﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using VP_QM_winform.VO;

namespace VP_QM_winform.ComManager
{
    public class VPBusManager
    {
        //TCP소켓 클라이언트 
        private TcpClient client;
        private NetworkStream stream;

        // 서버 연결 메서드 (한 번 연결 유지)
        public void Connect()
        {
            try
            {
                client = new TcpClient("127.0.0.1", 51900);
                stream = client.GetStream();
                Console.WriteLine("[CLIENT] Connected to server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to connect to server: {ex.Message}");
            }
        }

        // 서버와의 연결 종료 메서드
        public void Disconnect()
        {
            try
            {
                stream?.Close();
                client?.Close();
                Console.WriteLine("[CLIENT] Disconnected from server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
        }

        // 데이터 전송 메서드
        public void SendData(VisionCumVO visionCumVO)
        {
            try
            {
                // VisionCumVO 객체를 바이트 배열로 변환
                byte[] message = CreateMessage(visionCumVO);

                // 데이터 전송
                Console.WriteLine("[CLIENT] Sending data to server...");
                stream.Write(message, 0, message.Length);
                Console.WriteLine("[CLIENT] Data sent.");

                // 서버 응답 수신
                byte[] responseBuffer = new byte[1024];
                int bytesRead = stream.Read(responseBuffer, 0, responseBuffer.Length);
                string response = Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);
                Console.WriteLine($"[CLIENT] Server response: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
        }

        // 데이터 변환 메서드 (VisionCumVO -> 바이트 배열)
        private byte[] CreateMessage(VisionCumVO visionCumVO)
        {
            // 데이터 변환
            byte[] lineIdBytes = Encoding.ASCII.GetBytes(visionCumVO.LineId.PadRight(4, '\0')); // LineId (4바이트)
            byte[] timeBytes = BitConverter.GetBytes(
                (long)(visionCumVO.Time.ToUniversalTime() - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds * 1000
            ); // Time (8바이트, PostgreSQL Epoch 기준 마이크로초)
            byte[] lotIdBytes = Encoding.ASCII.GetBytes(visionCumVO.LotId.PadRight(10, '\0')); // LotId (10바이트)
            byte[] shiftBytes = Encoding.ASCII.GetBytes(visionCumVO.Shift.PadRight(4, '\0')); // Shift (4바이트)
            byte[] employeeNumberBytes = BitConverter.GetBytes((long)visionCumVO.EmployeeNumber); // EmployeeNumber (8바이트)
            byte[] totalBytes = BitConverter.GetBytes(visionCumVO.Total); // Total (4바이트)

            // 페이로드 생성
            byte[] payload = new byte[38];
            Buffer.BlockCopy(lineIdBytes, 0, payload, 0, lineIdBytes.Length);
            Buffer.BlockCopy(timeBytes, 0, payload, 4, timeBytes.Length);
            Buffer.BlockCopy(lotIdBytes, 0, payload, 12, lotIdBytes.Length);
            Buffer.BlockCopy(shiftBytes, 0, payload, 22, shiftBytes.Length);
            Buffer.BlockCopy(employeeNumberBytes, 0, payload, 26, employeeNumberBytes.Length);
            Buffer.BlockCopy(totalBytes, 0, payload, 34, totalBytes.Length);

            // 헤더 생성
            byte messageLength = (byte)(2 + payload.Length); // Header(2바이트) + Payload
            byte messageVersion = 1;

            byte[] header = new byte[2];
            header[0] = messageLength;
            header[1] = messageVersion;

            // 전체 메시지 생성
            byte[] message = new byte[header.Length + payload.Length];
            Buffer.BlockCopy(header, 0, message, 0, header.Length);
            Buffer.BlockCopy(payload, 0, message, header.Length, payload.Length);

            return message;
        }

    }
}