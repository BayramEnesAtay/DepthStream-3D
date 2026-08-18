using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Globalization;
using System;

public class UDPReceiver : MonoBehaviour
{
    [Header("GPU Rendering Ayarları")]
    public Material PointCloudMat;
    public Mesh instanceMesh; // Sahnede görünmeyen, sadece bellekte duran basit bir Küp (Cube) veya Quad

    // Shader'daki PointData struct'ının C# karşılığı
    struct PointData
    {
        public Vector3 position;
        public Vector3 color;
    }

    int gridSize = 50;
    float spacing = 0.14f;

    Thread receiveThread;
    UdpClient client;
    public int port = 5051;
    bool startReceiving = true;

    private string latestData = "";
    private bool isDataNew = false;

    // GPU bellek tamponları
    private ComputeBuffer pointBuffer;//bu ise noktaların postıon ve color degıskenını tuttugumuz GPU tarafındakı dızımız.
    private PointData[] pointDataArray;//bu ise noktaların postıon ve color degıskenını tuttugumuz CPU tarafındakı dızımız.
    private ComputeBuffer argsBuffer;// bu ve args dızısı bıze drawinstance ıcın gereklıdır(kac tane instance vs)
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    void Start()
    {
        int totalPoints = gridSize * gridSize;
        pointDataArray = new PointData[totalPoints];

        // 24 byte (Vector3 pos + Vector3 col = 6 adet float * 4 byte)
        pointBuffer = new ComputeBuffer(totalPoints, 24);

        // GPU'ya kaç adet kopya (instance) çizeceğini söyleyen argüman tamponu
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        args[0] = (instanceMesh != null) ? instanceMesh.GetIndexCount(0) : 0;
        args[1] = (uint)totalPoints;
        argsBuffer.SetData(args);

        // Buffer'ı materyale bağlıyoruz
        PointCloudMat.SetBuffer("_PointBuffer", pointBuffer);

        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (startReceiving)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                latestData = Encoding.UTF8.GetString(data);
                isDataNew = true;
            }
            catch (Exception) { }
        }
    }

    void Update()
    {
        if (isDataNew)
        {
            ProcessDataToBuffer(latestData);
            isDataNew = false;
        }

        // Unity CPU'yu hiç yormadan, donanımsal olarak doğrudan çizim emri veriyor
        Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, PointCloudMat, new Bounds(Vector3.zero, new Vector3(100, 100, 100)), argsBuffer);
    }

    void ProcessDataToBuffer(string dataString)
    {
        string[] stringValues = dataString.Split(',');

        if (stringValues.Length == pointDataArray.Length * 4)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    int index = y * gridSize + x;
                    int dataIndex = index * 4;

                    if (float.TryParse(stringValues[dataIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out float zValue))
                    {
                        // Shader renkleri 0-1 aralığında bekler, byte değerleri float'a oranlıyoruz
                        float r = byte.Parse(stringValues[dataIndex + 1]) / 255f;
                        float g = byte.Parse(stringValues[dataIndex + 2]) / 255f;
                        float b = byte.Parse(stringValues[dataIndex + 3]) / 255f;

                        pointDataArray[index].position = new Vector3(x * spacing, y * -spacing, -zValue);
                        pointDataArray[index].color = new Vector3(r, g, b);
                    }
                }
            }
            // Tüm array hazırlandıktan sonra tek seferde GPU'ya gönderiyoruz
            pointBuffer.SetData(pointDataArray);
        }
    }

    void OnDisable()
    {
        startReceiving = false;
        if (client != null) client.Close();
        if (receiveThread != null) receiveThread.Abort();

        // Memory leak (bellek sızıntısı) olmaması için buffer'ları mutlaka serbest bırakıyoruz
        if (pointBuffer != null) pointBuffer.Release();
        if (argsBuffer != null) argsBuffer.Release();
    }
}