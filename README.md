# DepthStream-3D: Real-Time Depth Mapping

![Unity](https://img.shields.io/badge/Unity-2022+-black?style=flat-square&logo=unity)
![Python](https://img.shields.io/badge/Python-3.9+-blue?style=flat-square&logo=python)
![PyTorch](https://img.shields.io/badge/PyTorch-AI-red?style=flat-square&logo=pytorch)
![HLSL](https://img.shields.io/badge/HLSL-Compute_Shader-purple?style=flat-square)

<img width="648" height="356" alt="Animation" src="https://github.com/user-attachments/assets/e375fd8a-a82c-41b9-ba6c-1ccaf58535b9" />

## 📌 Proje Özeti
**DepthStream-3D**, standart 2 boyutlu monoküler kameralardan alınan görüntüleri (örneğin bina içi koridorlar, kapalı alanlar), yapay zeka destekli derinlik kestirimi (Depth Estimation) kullanarak anlık olarak 3 boyutlu nokta bulutlarına (Point Cloud) dönüştüren ve GPU üzerinde renderlayan bir sistem mimarisidir.

## ⚙️ Sistem Mimarisi ve Optimizasyon
Proje, CPU darboğazlarını aşmak ve maksimum performansı sağlamak için iki ayrı modülden oluşmaktadır:

### 1. Yapay Zeka ve Veri Akışı (Python Backend)
- **Model:** Intel DPT-Large mimarisi kullanılarak tek lensli kamera görüntülerinden yüksek isabetli Z-ekseni matrisleri çıkarılır.
- **Ağ Protokolü:** Düşük gecikme (low-latency) gereksinimleri için veriler paketlenerek **UDP (User Datagram Protocol)** üzerinden Unity istemcisine iletilir.

### 2. Donanımsal Renderlama (Unity URP & HLSL)
- **GPU Instancing:** Binlerce `GameObject` üretmek yerine, doğrudan ekran kartı çekirdeklerini hedef alan alt seviye bir render hattı kurgulanmıştır.
- **Compute Buffers:** UDP üzerinden gelen derinlik matrisleri, **HLSL (High-Level Shader Language)** ile yazılmış özel shader'lara `StructuredBuffer` aracılığıyla gönderilir.
- Sistem, CPU'yu yormadan on binlerce topografik noktayı 60+ FPS ile çizebilmektedir.

## 🛠️ Kurulum Gereksinimleri
Projeyi yerel ortamınızda çalıştırmak için aşağıdaki kütüphanelerin ve ortamların kurulu olması gerekmektedir:

**2. Unity Sahne ve Materyal Kurulumu (Önemli)**
Projeyi ilk açtığınızda görselleştirmenin doğru çalışması için şu bağlantıları kontrol edin/yapın:
* `Assets` klasöründeki `PointCloudMat` materyaline tıklayın ve Inspector panelinden Shader olarak klasördeki `PointShader` dosyasını seçip atayın.
* Sahnenizdeki yönetici GameObject'i seçin ve ana UDP/Nokta Bulutu **C# scriptini** bu objeye Component olarak ekleyin.
* Scriptin Inspector panelindeki `Material` yuvasına, az önce shader atadığınız `PointCloudMat` dosyasını sürükleyip bırakın.

**Python Tarafı İçin:**
```bash
pip install torch transformers opencv-python numpy
```

**Unity Tarafı İçin:**
- Unity 2022 veya daha güncel bir sürüm.
- Proje URP (Universal Render Pipeline) mimarisi üzerinde çalışmaktadır.

## 🚀 Nasıl Çalıştırılır?
Veri akışında kopma yaşanmaması için uygulamanın aşağıdaki sırayla başlatılması gerekmektedir:

1. Unity editörüne geçiş yapın ve Play butonuna basarak GPU destekli gerçek zamanlı 3D nokta bulutu renderlamasını başlatın.
2. Proje dizininde terminali açın ve yapay zeka sunucusunu başlatın:
```bash
   python sender.py
```
3. Modelin ağırlıkları yüklendikten sonra terminalde veri yayın akışının başladığına dair onay mesajını bekleyin.

