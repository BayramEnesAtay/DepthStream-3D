
import socket
import time
import cv2
import torch
import numpy as np
import os 
os.environ["HF_HOME"] = "D:/AImodel" 
os.environ["HUGGINGFACE_HUB_CACHE"] = "D:/AImodel"
from transformers import DPTImageProcessor, DPTForDepthEstimation
##DPT modelini kullanıyoruz.
UDP_IP = "127.0.0.1"
UDP_PORT = 5051

def main():
    try:
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

        print("1. Yapay Zeka Modeli İndiriliyor/Yükleniyor (Bu işlem ilk seferde biraz sürebilir)...")
        # Intel'in MiDaS modelini Hugging Face üzerinden çekiyoruz
        processor = DPTImageProcessor.from_pretrained("Intel/dpt-large")
        model = DPTForDepthEstimation.from_pretrained("Intel/dpt-large")

        print("2. Model Yüklendi! Fotoğraf işleniyor...")

        # Resmi oku
        img = cv2.imread('foto.jpg')
        if img is None:
            print("HATA: foto.jpg bulunamadı!")
            exit()

        # Unity tarafına renkleri doğru göndermek için BGR'den RGB'ye çevir
        img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)

        # YZ'nin daha rahat işlemesi için görüntüyü orijinal haliyle hazırlıyoruz
        # ÖN İŞLEME (Preprocessing)
        inputs = processor(images=img_rgb, return_tensors="pt")

        # ÇIKARIM (Inference) - Derinliği hesapla
        with torch.no_grad():
            outputs = model(**inputs)
        predicted_depth = outputs.predicted_depth

        # SON İŞLEME (Postprocessing)
        # Yapay zekanın ürettiği matrisi, bizim Unity'deki 50x50 küp grid'imize uyacak şekilde yeniden boyutlandırıyoruz
        prediction = torch.nn.functional.interpolate(
            predicted_depth.unsqueeze(1),
            size=(50, 50),
            mode="bicubic",
            align_corners=False,
        )

        # Çıkan Tensor verisini standart Numpy dizisine (matrisine) çeviriyoruz
        depth_map = prediction.squeeze().cpu().numpy()

        # Derinlik değerleri çok yüksek/düşük olabilir. Hepsini Unity için 0.0 ile 2.0 metre arasına sıkıştırıyoruz (Normalize)
        depth_min = depth_map.min()
        depth_max = depth_map.max()
        depth_normalized = (depth_map - depth_min) / (depth_max - depth_min) * 2.0

        # Renkler için de 50x50 boyutunda küçük bir resim oluşturuyoruz
        img_small = cv2.resize(img_rgb, (50, 50))

        print("3. Matris oluşturuldu. Unity'e gönderim başlıyor...")

        packet_data = []

        # Matrisi piksel piksel tarayıp UDP paketimizi hazırlıyoruz
        for y in range(50):
            for x in range(50):
                r, g, b = img_small[y, x]
            
                # Modelin bulduğu derinlik değerini kullanıyoruz (Tersine çeviriyoruz ki yakın olan kabarsın)
                z = 2.0 + depth_normalized[y, x] ## modelde genellikle yuksek deger=kameradan uzak anlamına gelir.
                
                packet_data.append(f"{z:.2f},{r},{g},{b}")

        message = ",".join(packet_data)
        while True:
            sock.sendto(message.encode('utf-8'), (UDP_IP, UDP_PORT))
            time.sleep(1/15)
    except KeyboardInterrupt:
        print("Yayın durduruldu.")
        sock.close()
    except Exception as e:
        print(f"Hata oluştu: {e}")
        sock.close()


if __name__ == "__main__":
    main()