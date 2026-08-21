<template>
  <div class="album-page" ref="container">
    <!-- ===== 相册列表 ===== -->
    <section v-if="!currentAlbum" class="stagger-item">
      <div class="page-head">
        <h2>双人相册</h2>
        <NButton type="primary" size="small" @click="showCreate = true">＋ 新建相册</NButton>
      </div>

      <IndSkeleton v-if="loading" variant="grid" :rows="6" :columns="3" />
      <IndEmpty
        v-else-if="albums.length === 0"
        title="还没有相册"
        desc="点右上角新建一个，把你们的回忆收进来吧"
        actionText="＋ 新建相册"
        @action="showCreate = true"
      />

      <div v-else class="album-grid" ref="albumGrid">
        <div
          v-for="a in albums"
          :key="a.id"
          class="love-card album-card stagger-item"
          @click="openAlbum(a)"
        >
          <div class="album-cover">
            <img v-if="a.cover" :src="a.cover" :alt="a.albumName" loading="lazy" />
            <div v-else class="album-cover-ph"></div>
          </div>
          <div class="album-meta">
            <div class="album-name title-clamp">{{ a.albumName }}</div>
            <div class="sub-text">{{ a.imageCount }} 张 · 与 TA 共享</div>
          </div>
        </div>
      </div>
    </section>

    <!-- ===== 相册内：图片网格 ===== -->
    <section v-else class="stagger-item">
      <div class="page-head">
        <NButton quaternary size="small" @click="backToList">‹ 返回</NButton>
        <h2 class="album-title">{{ currentAlbum.albumName }}</h2>
        <NTag v-if="currentAlbum.remark" :bordered="false" type="warning">{{ currentAlbum.remark }}</NTag>
        <!-- 桌面端：页头醒目的上传按钮 -->
        <NUpload
          v-if="!isMobile()"
          multiple
          :max="20"
          accept="image/*"
          :show-file-list="false"
          :custom-request="customRequest"
          class="head-upload"
        >
          <NButton type="primary" size="small">＋ 上传图片</NButton>
        </NUpload>
      </div>

      <IndSkeleton v-if="imgLoading" variant="grid" :rows="6" :columns="3" />

      <IndEmpty
        v-else-if="images.length === 0"
        title="这个相册还没有照片"
        desc="支持 jpg / png / gif / webp，单张 ≤ 5MB，最多 20 张"
      >
        <template #action>
          <NUpload
            multiple
            :max="20"
            accept="image/*"
            :show-file-list="false"
            :custom-request="customRequest"
          >
            <NButton type="primary">＋ 上传第一张照片</NButton>
          </NUpload>
        </template>
      </IndEmpty>

      <div v-else class="img-grid">
        <!-- 桌面端：网格首位的“添加照片”磁贴，入口更醒目 -->
        <NUpload
          v-if="!isMobile()"
          multiple
          :max="20"
          accept="image/*"
          :show-file-list="false"
          :custom-request="customRequest"
          class="add-tile-wrap"
        >
          <div class="add-tile">
            <div class="add-plus">＋</div>
            <div class="add-txt">添加照片</div>
          </div>
        </NUpload>
        <div
          v-for="img in images"
          :key="img.id"
          class="img-cell love-card"
          @click="openLightbox(img)"
        >
          <img class="thumb" :src="img.url || img.imagePath" :alt="img.remark || 'photo'" loading="lazy" />
          <div class="img-cap" v-if="img.remark">{{ img.remark }}</div>
          <button class="img-fav" :class="{ on: favs.has(img.id) }" @click.stop="toggleFav(img)" :aria-label="favs.has(img.id) ? '取消收藏' : '收藏'"><Heart :size="14" :fill="favs.has(img.id) ? 'currentColor' : 'none'" /></button>
          <NButton class="img-del" size="tiny" quaternary circle @click.stop="removeImage(img)">✕</NButton>
        </div>
      </div>

      <AlbumLightbox
        :images="lbImages"
        v-model="lightboxIndex"
        :favs="favs"
        @toggle-fav="onLightboxFav"
      />

      <!-- 移动端固定底部上传 -->
      <div v-if="isMobile()" class="upload-fab">
        <NUpload
          multiple
          :max="20"
          accept="image/*"
          :show-file-list="false"
          :custom-request="customRequest"
        >
          <NButton type="primary" block>＋ 上传本地图片（最多 20 张）</NButton>
        </NUpload>
      </div>
    </section>

    <!-- ===== 新建相册弹窗 ===== -->
    <NModal v-model:show="showCreate" title="新建相册" preset="card" class="album-modal" style="width: 92%; max-width: 420px;">
      <NForm ref="formRef" :model="form" label-placement="top">
        <NFormItem label="相册名称" :rule="requiredRule('给相册起个名字吧～')">
          <NInput v-model:value="form.albumName" placeholder="例如：我们的旅行" />
        </NFormItem>
        <NFormItem label="封面图（可选）">
          <ImageField v-model="form.cover" />
        </NFormItem>
        <NFormItem label="简介（可选）">
          <NInput v-model:value="form.remark" type="textarea" placeholder="记录这个相册的故事" />
        </NFormItem>
      </NForm>
      <template #footer>
        <div class="modal-foot">
          <NButton @click="showCreate = false">取消</NButton>
          <NButton type="primary" :loading="creating" @click="createAlbum">创建</NButton>
        </div>
      </template>
    </NModal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted, nextTick } from 'vue';
import {
  NButton, NModal, NForm, NFormItem, NInput, NUpload, NTag,
} from 'naive-ui';
import type { UploadCustomRequestOptions } from 'naive-ui';
import type { AlbumDto, AlbumReq, ImageDto, ApiResult } from '@/types';
import * as albumApi from '@/api/album';
import { isMobile } from '@/composables/useDevice';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useSettingStore } from '@/store/settingStore';
import AlbumLightbox from '@/components/album/AlbumLightbox.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import { feedback } from '@/utils/feedback';
import { Heart } from 'lucide-vue-next';
import { requiredRule } from '@/utils/formRules';
import ImageField from '@/components/Common/ImageField.vue';

const setting = useSettingStore();

const formRef = ref();

const container = ref<HTMLElement>();
useStaggerEnter(container, '.stagger-item', { stagger: 0.08, y: 16 });

const loading = ref(true);
const albums = ref<AlbumDto[]>([]);

const albumGrid = ref<HTMLElement | null>(null);
let parallaxRAF = 0;
function applyParallax() {
  const grid = albumGrid.value ?? document.querySelector<HTMLElement>('.album-grid');
  if (!grid) return;
  if (setting.reduceMotion) {
    grid.querySelectorAll<HTMLElement>('.album-cover img').forEach((img) => { img.style.transform = ''; });
    return;
  }
  const vh = window.innerHeight || document.documentElement.clientHeight;
  grid.querySelectorAll<HTMLElement>('.album-cover').forEach((card) => {
    const img = card.querySelector<HTMLElement>('img');
    if (!img) return;
    const r = card.getBoundingClientRect();
    const center = r.top + r.height / 2;
    // 卡片中心相对视口中心的归一化偏移（-0.5 ~ 0.5），乘以系数得到 ±12px 视差
    const delta = (center - vh / 2) / vh;
    img.style.transform = `translateY(${(-delta * 24).toFixed(2)}px)`;
  });
}
function onScrollParallax() {
  if (parallaxRAF) return;
  parallaxRAF = window.requestAnimationFrame(() => {
    parallaxRAF = 0;
    applyParallax();
  });
}

const currentAlbum = ref<AlbumDto | null>(null);
const images = ref<ImageDto[]>([]);
const imgLoading = ref(false);

const showCreate = ref(false);
const creating = ref(false);
const form = reactive<AlbumReq>({ albumName: '', cover: '', remark: '' });
const favs = ref<Set<number>>(new Set());
function toggleFav(img: ImageDto) {
  const s = new Set(favs.value);
  s.has(img.id) ? s.delete(img.id) : s.add(img.id);
  favs.value = s;
}

const lightboxIndex = ref(-1);
const lbImages = computed(() =>
  images.value.map((i) => ({ id: i.id, url: i.url || i.imagePath, remark: i.remark }))
);
function openLightbox(img: ImageDto) {
  lightboxIndex.value = images.value.findIndex((i) => i.id === img.id);
}
function onLightboxFav(id: number) {
  const img = images.value.find((i) => i.id === id);
  if (img) toggleFav(img);
}

async function loadAlbums() {
  loading.value = true;
  try {
    const res = await albumApi.listAlbum({ page: 1, pageSize: 50 });
    albums.value = (res.data as ApiResult<{ items: AlbumDto[] }>).data?.items ?? [];
  } finally {
    loading.value = false;
  }
  // 必须在 loading=false（网格真实渲染）之后、再 nextTick，applyParallax 才能取到 .album-grid
  await nextTick();
  applyParallax();
}

async function openAlbum(a: AlbumDto) {
  currentAlbum.value = a;
  imgLoading.value = true;
  images.value = [];
  try {
    const res = await albumApi.listImages(a.id);
    images.value = (res.data as ApiResult<ImageDto[]>).data ?? [];
  } finally {
    imgLoading.value = false;
  }
}

function backToList() {
  currentAlbum.value = null;
  loadAlbums();
}

async function createAlbum() {
  try {
    await formRef.value?.validate();
  } catch {
    return;
  }
  creating.value = true;
  try {
    const res = await albumApi.createAlbum({ ...form });
    const dto = (res.data as ApiResult<AlbumDto>).data;
    showCreate.value = false;
    form.albumName = ''; form.cover = ''; form.remark = '';
    if (dto) {
      // 新建后直接进入相册，立即展示上传入口，避免停在列表找不到上传
      await openAlbum(dto);
    } else {
      await loadAlbums();
    }
    feedback.created('相册');
  } finally {
    creating.value = false;
  }
}

async function customRequest(opt: UploadCustomRequestOptions) {
  const file = opt.file.file;
  if (!file || !currentAlbum.value) { opt.onError(); return; }
  try {
    const res = await albumApi.uploadImage(currentAlbum.value.id, file);
    const dto = (res.data as ApiResult<ImageDto>).data;
    if (dto) images.value.unshift(dto);
    feedback.saved('照片');
    opt.onFinish();
  } catch {
    opt.onError();
  }
}

async function removeImage(img: ImageDto) {
  await albumApi.deleteImage(img.id);
  images.value = images.value.filter((x) => x.id !== img.id);
  if (currentAlbum.value) currentAlbum.value.imageCount = Math.max(0, currentAlbum.value.imageCount - 1);
}

import { useRealtime, overlaySyncMap } from '@/composables/useRealtime';
const { useModuleSync } = useRealtime();
onMounted(async () => {
  await loadAlbums();
  useModuleSync('album', { items: albums, getId: i => i.id, load: loadAlbums, map: overlaySyncMap });
  window.addEventListener('scroll', onScrollParallax, { passive: true });
  window.addEventListener('resize', onScrollParallax);
});
onUnmounted(() => {
  window.removeEventListener('scroll', onScrollParallax);
  window.removeEventListener('resize', onScrollParallax);
  if (parallaxRAF) window.cancelAnimationFrame(parallaxRAF);
});
</script>

<style scoped>
.album-page { max-width: 960px; margin: 0 auto; }
.page-head { display: flex; align-items: center; gap: 12px; margin-bottom: 16px; }
.page-head h2 { margin: 0; font-size: 18px; }
.album-title { flex: 1; }

.album-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(160px, 1fr)); gap: 14px; }
.album-card { padding: 0; overflow: hidden; cursor: pointer; transition: transform var(--dur-micro) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love); }
.album-cover { position: relative; aspect-ratio: 1 / 1; overflow: hidden; background: var(--color-ink-soft); }
.album-cover img { position: absolute; top: -15%; left: 0; width: 100%; height: 130%; object-fit: cover; will-change: transform; transition: transform var(--dur-micro) var(--ease-love); }
.album-cover-ph { width: 100%; height: 100%; display: grid; place-items: center; color: var(--color-ink-3); }
html:not(.reduce-motion) .album-card:hover { transform: translateY(-3px) scale(1.015); box-shadow: 0 6px 16px rgba(31, 41, 55, 0.08), 0 22px 50px -14px rgba(122, 100, 98, 0.28); }
.album-meta { padding: 10px 12px; }
.album-name { font-weight: 500; }

.img-grid { columns: 3; column-gap: 8px; }
.img-cell { position: relative; padding: 0; overflow: hidden; margin-bottom: 8px; break-inside: avoid; border-radius: var(--radius-md); }
.img-cell :deep(img) { width: 100%; height: auto; display: block; transition: transform var(--dur-micro) var(--ease-love); }
.thumb { width: 100%; height: auto; display: block; cursor: zoom-in; transition: transform var(--dur-micro) var(--ease-love); }
html:not(.reduce-motion) .img-cell:hover :deep(img) { transform: scale(1.05); }
.img-cap {
  position: absolute; left: 0; right: 0; bottom: 0; padding: 6px 8px; font-size: 12px; color: #fff;
  background: linear-gradient(transparent, rgba(0, 0, 0, 0.55)); pointer-events: none;
}
.img-fav {
  position: absolute; top: 6px; left: 6px; border: none; background: rgba(0, 0, 0, 0.4);
  width: 28px; height: 28px; border-radius: 50%; cursor: pointer; font-size: 14px; line-height: 1;
  opacity: 0; transition: opacity var(--dur-micro) var(--ease-love);
}
html:not(.reduce-motion) .img-cell:hover .img-fav, .img-fav.on { opacity: 1; }
.img-del { position: absolute; top: 6px; right: 6px; background: rgba(0,0,0,.45); color: #fff; }

.modal-foot { display: flex; justify-content: flex-end; gap: 10px; }
:global(.album-modal) { padding: 0 !important; }

.upload-fab {
  position: fixed; left: 16px; right: 16px;
  bottom: calc(env(safe-area-inset-bottom) + 16px);
  z-index: 20;
}

/* 空态上传 */
.upload-empty { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 28px 0; }
.upload-empty p { margin: 0; color: var(--color-ink-3); }
.upload-hint { font-size: 12px; color: var(--color-ink-3); margin: 4px 0 0; }

/* 网格首位的"添加照片"磁贴 */
.add-tile-wrap { margin-bottom: 8px; break-inside: avoid; }
.add-tile {
  width: 100%; aspect-ratio: 1 / 1; display: flex; flex-direction: column;
  align-items: center; justify-content: center; gap: 6px; cursor: pointer;
  border: 2px dashed var(--color-accent-soft); border-radius: var(--radius-md);
  color: var(--color-accent); background: var(--color-accent-soft);
  transition: transform var(--dur-micro) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love);
}
html:not(.reduce-motion) .add-tile:hover { transform: scale(1.03); box-shadow: 0 4px 12px rgba(31, 41, 55, 0.06), 0 18px 44px -12px rgba(122, 100, 98, 0.22); }
.add-plus { font-size: 30px; line-height: 1; }
.add-txt { font-size: 13px; font-weight: 500; }
:deep(.add-tile-wrap .n-upload-trigger) { width: 100%; height: 100%; }
:deep(.head-upload) { margin-left: auto; }
@media (max-width: 767px) {
  :global(.album-modal) { width: 100vw !important; max-width: 100vw !important; height: 100dvh; margin: 0; border-radius: 0; }
  .album-grid { grid-template-columns: repeat(2, 1fr); }
  .img-grid { columns: 2; }
}
</style>
