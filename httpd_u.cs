// 2016.06.19(Sun) subset http server.
//
//

using System;
using System.Drawing;
using System.IO;

namespace httpd
{
    class StartClass
    {
        static void Main(string[] args)
        {
            int port = 80;
            if (args.Length != 0) {
                
                if (args[0] == "?"){
                    Console.WriteLine("<number> :Listen Port Number : default 80");
                    return;//end of program
                } 

                int p = -1;
                try{ 
                    p = int.Parse(args[0]);
                }catch(System.FormatException e){
                    Console.WriteLine("error :Not Number.");
                    Console.WriteLine(e);                      
                    //return;
                }    
                
                if ((p >0)&&(p<65536))
                {
                    port = p;
                }else{
                    Console.WriteLine("error :Port Number Out of Range : Set Default. ");                        
                }
            }

            Console.WriteLine("main start.");

            TCPServer a = new TCPServer();
            a.Open(port); // telnet 23 / http 80

            Boolean rflag;
            do { rflag = a.AcceptClient(); }
            while (rflag);

            a.Close();

            Console.WriteLine("main end.");
        }
    }

    // TCP
    //
    class TCPServer
    {
        private System.Net.Sockets.TcpListener Listener;
        private System.Net.Sockets.TcpClient Client;

        private MessageParser msg = new MessageParser();

        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }

        public TCPServer()
        {
            //default Infinite(non Timeout)
            ReadTimeout = 10000; //ms
            WriteTimeout = 10000; //ms

            Console.WriteLine("サーバを開始します。");
        }

        public void Open(int portNumber)
        {
            System.Net.IPAddress ipAdd = System.Net.IPAddress.Any;

            Listener = new System.Net.Sockets.TcpListener(ipAdd, portNumber);

            Listener.Start();

            Console.WriteLine(
                string.Format("Listenを開始しました({0},{1})。",
                ((System.Net.IPEndPoint)Listener.LocalEndpoint).Address,
                ((System.Net.IPEndPoint)Listener.LocalEndpoint).Port)
            );
        }

        public void Close()
        {
            Listener.Stop();
            Console.WriteLine("Listenerを閉じました。");
        }

        public Boolean AcceptClient()
        {
            Console.WriteLine("クライアントと接続待ち。");

            Client = Listener.AcceptTcpClient();

            Console.WriteLine(
                string.Format("クライアント({0},{1})と接続しました。",
                ((System.Net.IPEndPoint)Client.Client.RemoteEndPoint).Address,
                ((System.Net.IPEndPoint)Client.Client.RemoteEndPoint).Port)
            );

            System.Net.Sockets.NetworkStream ns = Client.GetStream();

            ns.ReadTimeout = ReadTimeout;
            ns.WriteTimeout = WriteTimeout;

            System.Text.Encoding enc = System.Text.Encoding.UTF8;

            Boolean disconnected = false;

            do
            {
                System.IO.MemoryStream ms = new System.IO.MemoryStream();

                int resSize = 0;
                byte[] resBytes = new byte[1024];

                try
                {
                    do
                    {
                        resSize = ns.Read(resBytes, 0, resBytes.Length);
                        if (resSize == 0)
                        {
                            disconnected = true;
                            Console.WriteLine("クライアントが切断しました。");
                            break;
                        }

                        ms.Write(resBytes, 0, resSize);
                        //ns.Write(resBytes, 0, resSize);
                    } while (ns.DataAvailable || resBytes[resSize - 1] != '\n');
                }
                catch (IOException e)
                {
                    disconnected = true;
                    Console.WriteLine("Read Timeoutで切断しました。" + e);
                }

                string resMsg = enc.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                ms.Close();

                //Console.WriteLine("[" + resMsg + "]");

                if (disconnected)
                {
                    //メッセージ解析

                    //返信処理

                    Console.WriteLine("通信終了。");
                }
                else
                {
                    //メッセージ解析

                    disconnected = !msg.Input(resMsg);

                    //msg.Input(resMsg);
                    //返信処理

                    string sendMsg = msg.Output();
                    byte[] sendByte = enc.GetBytes(sendMsg + "\n");
                    byte[] sendData = msg.OutputBinary();

                    try
                    {
                        ns.Write(sendByte, 0, sendByte.Length);
                        ns.Write(sendData, 0, sendData.Length);
                    }
                    catch (IOException e)
                    {
                        disconnected = true;
                        Console.WriteLine("Write Timeoutで切断しました。" + e);
                    }
                    //Console.WriteLine("[" + sendMsg + "]" + " 送信。");
                    Console.WriteLine("送信 text:" + sendByte.Length.ToString() + "bytes, binary:" +sendData.Length.ToString());

                }
            } while (!disconnected);

            ns.Close();
            Client.Close();

            return true;
        }
    }

    // http 0.9 parser
    //
    class MessageParser
    {
        private string retmsg;
        private string CRLF = "\r\n";

        private Byte[] retByte;

        public Boolean Input(string msg)
        {
            //
            //header
            string[] rowStr = msg.Split('\n');
            string[] colStr;

            retmsg = "";
            retByte = new Byte[] { };

            string connect = "Close";
            foreach (string s in rowStr)
            {
                string ws = s.Replace("\r", "");
                colStr = ws.Split(':');
                ws = "";

                if (colStr[0].CompareTo("Connection") == 0)
                {
                    connect = colStr[1];
                    Console.WriteLine(colStr[1]);
                }
            }

            colStr = rowStr[0].Split(' ');

            Boolean statusflag = true;

            //msg = msg.TrimEnd('\n');
            //msg = msg.TrimEnd('\r');

            msg = colStr[0];

            Console.WriteLine("[" + msg + "]");

            switch (msg)
            {
                case "GET":
                    /* HTTP/0.9 404 Not Found
                    指定されたURIのリソースを取り出す。HTTPの最も基本的な動作で、HTTP/0.9では唯一のメソッド。
                    */

                    Console.WriteLine("filePath" + colStr[1]);

                    string fileName = @colStr[1];
                    fileName = fileName.TrimStart('/');

                    if (fileName.Length == 0) { fileName = "index.html"; }

                    if (System.IO.File.Exists(fileName))
                    {
                        Console.WriteLine("'" + fileName + "'は存在します。");
                        System.IO.FileInfo fi = new System.IO.FileInfo(fileName);
                        //ファイルのサイズを取得
                        Console.WriteLine("ファイルサイズ：" + fi.Length + " bytes");

                        colStr = fileName.Split('.');

                        string fileType = colStr[colStr.Length - 1];

                        Console.WriteLine("ファイルタイプ：" + fileType);

                        switch (fileType)
                        {
                            case "txt":
                            case "html":
                            case "htm":
                            case "css":
                                //System.Text.Encoding enc = System.Text.Encoding.UTF8;
                                System.Text.Encoding enc = System.Text.Encoding.GetEncoding("Shift_JIS");
                                //テキストファイルの中身をすべて読み込む
                                string c_str = System.IO.File.ReadAllText(fileName, enc);

                                retByte = enc.GetBytes(c_str);

                                retmsg = "HTTP/0.9 200 OK" + CRLF;
                                //retmsg += "Content-Type: text/html; charset=UTF-8" + CRLF;
                                //retmsg += "Server: private_httpd" + CRLF;
                                //retmsg += "Status: 200 OK" + CRLF;
                                retmsg += "Content-Length: " + retByte.Length.ToString() + CRLF;
                                retmsg += "Connection: " + connect + CRLF;

                                retmsg += CRLF;

                                break;

                            case "jpg":
                            case "jpeg":
                            case "png":
                            case "gif":
                            case "ico":

                                retByte = System.IO.File.ReadAllBytes(fileName);

                                retmsg = "HTTP/0.9 200 OK" + CRLF;
                                //retmsg += "Server: private_httpd" + CRLF;
                                //retmsg += "Status: 200 OK" + CRLF;
                                retmsg += "Content-Length: " + retByte.Length.ToString() + CRLF;
                                retmsg += "Connection: " + connect + CRLF;

                                //retmsg += CRLF;

                                break;
                            default:

                                retmsg = "HTTP/0.9 415 Unsupported Media Type" + CRLF;
                                retmsg += "Server: private_httpd" + CRLF;
                                retmsg += "Status: 415 Unsupported Media Type" + CRLF;
                                retmsg += "Content-Length: " + retByte.Length.ToString() + CRLF;
                                retmsg += "Connection: " + connect + CRLF;

                                break;
                        }
                    }
                    else
                    {
                        Console.WriteLine("'" + fileName + "'は存在しません。");

                        retmsg = "HTTP/0.9 404 Not Found" + CRLF;
                        retmsg += "Server: private_httpd" + CRLF;
                        retmsg += "Status: 404 Not Found" + CRLF;
                        retmsg += "Connection: " + connect + CRLF;


                    }

                    if (connect == "Keep-Alive")
                    {
                        statusflag = true;
                    }
                    else
                    {
                        statusflag = false;
                    }
                    break;
                case "POST":
                /* HTTP/1.0
                GETとは反対にクライアントがサーバにデータを送信する。
                Webフォームや電子掲示板への投稿などで使用される。
                GETの場合と同じく、サーバはクライアントにデータを返すことができる。
                */
                case "PUT":
                /* HTTP/1.0 201 Created 202 Accepted
                指定したURIにリソースを保存する。
                URIが指し示すリソースが存在しない場合は、サーバはそのURIにリソースを作成する。
                画像のアップロードなどが代表的。
                */
                case "HEAD":
                /* HTTP/1.0 200 OK
                GETと似ているが、サーバはHTTPヘッダのみ返す。
                クライアントはWebページを取得せずともそのWebページが存在するかどうかを知ることができる。
                例えばWebページのリンク先が生きているか、データを全て取得することなく検証することができる
                */
                case "DELETE":
                // HTTP/1.0 指定したURIのリソースを削除する
                case "OPTIONS":

                // HTTP/1.1 サーバを調査する。例えば、サーバがサポートしているHTTPバージョンなどを知ることができる。
                case "TRACE":
                /* HTTP/1.1
                TRACEサーバまでのネットワーク経路をチェックする。
                サーバは受け取ったメッセージのそれ自体をレスポンスのデータにコピーして応答する。
                WindowsのTracertやUNIXのTracerouteとよく似た動作。
                */
                case "CONNECT":
                    /* HTTP/1.1
                    TRACEサーバまでのネットワーク経路をチェックする。サーバは受け取ったメッセージのそれ自体をレスポンスのデータにコピーして応答する。
                    WindowsのTracertやUNIXのTracerouteとよく似た動作。CONNECTTCPトンネルを接続する。
                    暗号化したメッセージをプロキシサーバを経由して転送する際に用いる。
                    */
                    retmsg = "HTTP/0.9 501 Not Implemented\n\n";
                    statusflag = false;
                    break;
                default:
                    retmsg = "HTTP/0.9 400 Bad Request\n\n";
                    statusflag = false;
                    break;
            }
            //true:接続継続   false:接続終了
            return statusflag;

        }

        public string Output()
        {
            //
            return retmsg;
        }

        public Byte[] OutputBinary()
        {

            return retByte;
        }

    }

}