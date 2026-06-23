export interface TutorialStep {
  title: string;
  description: string;
  mediaSrc?: string;
  mediaType?: 'image' | 'video';
}

export const codeGraphTutorialSteps: TutorialStep[] = 
[
  {
    title: 'Pengaturan & Keterangan Grafik',
    description: 'Atur visibilitas node dan tata letak grafik melalui menu di pojok kanan bawah. Lihat keterangan warna pada legenda untuk membedakan tipe file atau node.',
    mediaSrc: 'https://placehold.co/600x400?text=Pengaturan+%26+Keterangan+Grafik',
    mediaType: 'image'
  },
  {
    title: 'Pencarian Cepat',
    description: 'Gunakan fitur pencarian di pojok kiri atas atau tekan Ctrl+K untuk menemukan file atau node secara spesifik dalam graf.',
    mediaSrc: 'https://placehold.co/600x400?text=Pencarian+Cepat',
    mediaType: 'image'
  },
  {
    title: 'Navigasi Grafik',
    description: 'Scroll mouse untuk melakukan zoom in/out. Klik dan drag pada area kosong untuk menggeser (pan) tampilan grafik.',
    mediaSrc: 'https://placehold.co/600x400?text=Navigasi+Grafik',
    mediaType: 'image'
  },
  {
    title: 'Collapse & Expand Node',
    description: 'Fokuskan tampilan dengan menyembunyikan (collapse) atau menampilkan (expand) node relasi. Dapat diakses lewat Pengaturan atau menu konteks.',
    mediaSrc: 'https://placehold.co/600x400?text=Collapse+%26+Expand+Node',
    mediaType: 'image'
  },
  {
    title: 'Menu Konteks',
    description: 'Klik kanan pada sebuah node untuk membuka menu konteks. Anda dapat melihat relasi, membuka kode sumber, atau menyematkan (pin) menu.',
    mediaSrc: 'https://placehold.co/600x400?text=Menu+Konteks',
    mediaType: 'image'
  },
  {
    title: 'Penampil Kode (Code Viewer)',
    description: 'Klik kiri pada node file atau pilih \'Lihat Source Code\' dari menu konteks untuk membuka panel kode. Anda dapat melihat baris kode yang relevan dengan node tersebut.',
    mediaSrc: 'https://placehold.co/600x400?text=Penampil+Kode',
    mediaType: 'image'
  }
];
