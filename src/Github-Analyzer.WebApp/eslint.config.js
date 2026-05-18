import pluginVue from 'eslint-plugin-vue';
import vueTsEslintConfig from '@vue/eslint-config-typescript';
import betterTailwindcss from 'eslint-plugin-better-tailwindcss';

export default [
  // Ignore build artifacts
  { ignores: ['dist/**', 'dist-ssr/**', 'coverage/**'] },

  // Vue essential configuration
  ...pluginVue.configs['flat/essential'],

  // Vue TypeScript configuration
  ...vueTsEslintConfig(),

  // Custom rules matching the reference project + new formatting requests
  {
    name: 'custom-style',
    files: ['**/*.{js,ts,mts,tsx,vue}'],
    settings: {
      'better-tailwindcss': {
        entryPoint: './src/styles/main.css',
      },
    },
    plugins: {
      'better-tailwindcss': betterTailwindcss,
    },
    rules: {
      // ── FORMATTING DASAR ──────────────────────────────────────────────
      // Gaya kurung kurawal 'Allman' (buka kurung di baris baru)
      'brace-style': ['error', 'allman', { allowSingleLine: true }],

      // Arrow function tidak pakai kurung kurawal jika isinya hanya sebaris
      'arrow-body-style': ['error', 'as-needed'],

      // Bebaskan posisi baris baru setelah tanda panah arrow function (=>)
      'implicit-arrow-linebreak': 'off',

      // Bebaskan penggunaan baris baru di dalam deklarasi objek
      'object-curly-newline': 'off',

      // Wajibkan spasi di dalam kurung kurawal blok kode (contoh: { foo: bar })
      'block-spacing': ['error', 'always'],

      // Bebaskan pengaturan baris kosong antar statement kode
      'padding-line-between-statements': 'off',

      // Wajibkan indentasi menggunakan 2 spasi (termasuk pada blok switch case)
      indent: ['error', 2, { SwitchCase: 1 }],

      // Wajibkan penggunaan tanda kutip satu (single quote) pada string
      quotes: ['error', 'single'],

      // Wajibkan penulisan titik koma (semicolon) di akhir perintah
      semi: ['error', 'always'],

      // ── TAILWIND CSS ──────────────────────────────────────────────────
      // Mengurutkan posisi daftar class Tailwind secara standar dan otomatis
      'better-tailwindcss/enforce-consistent-class-order': 'warn',

      // Memecah barisan class Tailwind ke baris baru jika lebarnya melebihi 80 karakter
      'better-tailwindcss/enforce-consistent-line-wrapping': [
        'warn',
        {
          printWidth: 80,
          lineBreakStyle: 'auto',
        },
      ],

      // ── TYPESCRIPT & JAVASCRIPT ───────────────────────────────────────
      // Nonaktifkan no-unused-vars bawaan murni JS (karena menggunakan versi TypeScript)
      'no-unused-vars': 'off',

      // Mengabaikan peringatan variabel tidak dipakai apabila namanya diawali dengan underscore (_)
      '@typescript-eslint/no-unused-vars': [
        'warn',
        {
          argsIgnorePattern: '^_',
          varsIgnorePattern: '^_',
          caughtErrorsIgnorePattern: '^_',
        },
      ],

      // Mengizinkan penulisan tipe data bebas ("any") secara eksplisit
      '@typescript-eslint/no-explicit-any': 'off',

      // ── VUE.JS ────────────────────────────────────────────────────────
      // Bebaskan file komponen dari keharusan penamaan multi-kata (misal boleh 'Dashboard.vue')
      'vue/multi-word-component-names': 'off',

      // Indentasi baris kode HTML pada bagian template Vue diatur menggunakan 2 spasi (tidak meratakan sejajar dengan atribut pertama)
      'vue/html-indent': ['error', 2, { alignAttributesVertically: false }],

      // Pastikan ada jeda enter (baris baru) di pembuka dan penutup blok utama (<script>, <template>)
      'vue/block-tag-newline': [
        'error',
        { singleline: 'always', multiline: 'always' },
      ],
      
      // Letakkan tanda penutup kurung siku (>) atau self-closing (/>) di baris baru jika atribut multiline
      'vue/html-closing-bracket-newline': [
        'error',
        {
          singleline: 'never',
          multiline: 'always',
        },
      ],

      // Setap satu elemen atribut / properti di HTML / Vue wajib ditaruh dalam baris baru masing-masing
      'vue/max-attributes-per-line': [
        'error',
        {
          singleline: { max: 1 },
          multiline: { max: 1 },
        },
      ],
    },
  },
];
