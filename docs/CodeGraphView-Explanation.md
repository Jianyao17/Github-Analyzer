# Penjelasan CodeGraphView.vue

`CodeGraphView.vue` adalah komponen visualisasi graf interaktif yang dibangun menggunakan kombinasi **Vue 3 (Composition API)** dan **D3.js (Data-Driven Documents)**. Komponen ini dirancang untuk menampilkan struktur hirarki dan hubungan antar-komponen dalam kode sumber (codebase).

## Arsitektur Utama

### 1. Manajemen Dimensi & Responsivitas
*   **ResizeObserver**: Digunakan untuk memantau ukuran container secara real-time. Jika ukuran layar berubah (misal: sidebar ditutup), pusat gravitasi graf diperbarui secara otomatis.
*   **shallowRef**: Instance simulasi D3 disimpan dalam `shallowRef` untuk mencegah overhead performa dari sistem reaktivitas mendalam Vue pada objek internal D3 yang kompleks.

### 2. Mesin Fisika (D3-Force Simulation)
Menggunakan simulasi gaya untuk mengatur tata letak node secara otomatis:
*   **ManyBody (Repulsion)**: Memberikan gaya tolak-menolak antar node (set ke -1000) agar tidak saling menumpuk.
*   **Collide (Tabrakan)**: Memberikan radius fisik pada setiap node untuk mencegah label teks tumpang tindih.
*   **Link (Jarak)**: 
    *   **Hierarchy (Solid)**: Jarak pendek (100px) untuk menjaga struktur folder tetap rapat.
    *   **Usage (Dashed)**: Jarak panjang (200px) untuk menunjukkan hubungan lintas file tanpa mengacak-acak hirarki utama.
*   **Random Initialization**: Memberikan posisi acak awal di sekitar pusat layar untuk memecah simetri dan mencegah masalah "garis horizontal" di koordinat (0,0).

### 3. Visual Encoding
*   **Nodes**: 
    *   Ukuran dan warna dibedakan berdasarkan tipe: Namespace (Indigo), File (Emerald), Class (Blue), dan Function (Purple).
    *   Label teks memiliki bayangan tipis (`text-shadow`) untuk meningkatkan keterbacaan di atas latar belakang apa pun.
*   **Edges**:
    *   **SourceRelEdges (Solid)**: Mewakili hubungan kepemilikan (hirarki).
    *   **UseRelEdges (Dashed)**: Mewakili hubungan penggunaan atau pemanggilan fungsi.

### 4. Interaktivitas
*   **Zoom & Pan**: Mendukung navigasi luas menggunakan mouse wheel atau drag latar belakang.
*   **Drag & Drop**: Node dapat ditarik secara manual. Saat ditarik, posisi node akan "terkunci" (`fx`, `fy`) sampai dilepaskan.
*   **Legend Interaktif**: Menampilkan jumlah total node dan edge secara dinamis berdasarkan hasil analisis terbaru.

## Efisiensi Redraw
Komponen menggunakan `watch` dengan opsi `deep: true` pada props data. Setiap kali ada data analisis baru, SVG dibersihkan sepenuhnya sebelum menggambar ulang, memastikan transisi state yang bersih dan bebas dari kebocoran memori atau residu DOM.
