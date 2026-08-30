<template>
  <div class="album-page" ref="container">
    <!-- 品牌条 -->
    <div class="brand block">
      <h1 class="ind-label">ALBUM · 双人相册</h1>
      <span class="brand-status"><IndLed color="green" :size="9" /> 已同步</span>
    </div>

    <!-- ===== 相册列表 ===== -->
    <section v-if="!currentAlbum" class="stagger-item">
      <div class="page-head page-head-solo">
        <NButton type="primary" size="small" v-press-bounce @click="showCreate = true">＋ 新建相册</NButton>
      </div>

      <div class="album-toolbar">
        <NInput
          v-model:value="albumKeyword"
          placeholder="搜索相册名称"
          clearable
          size="small"
          aria-label="搜索相册名称"
          class="album-search"
        >
          <template #prefix><Search :size="15" :stroke-width="1.8" /></template>
        </NInput>
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
          v-for="a in filteredAlbums"
          :key="a.id"
          :data-album-id="a.id"
          class="love-card album-card stagger-item"
          role="button"
          tabindex="0"
          @click="openAlbum(a, $event)"
          @keydown.enter.prevent="openAlbum(a, $event)"
          @keydown.space.prevent="openAlbum(a, $event)"
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
          <NButton type="primary" size="small" v-press-bounce>＋ 上传图片</NButton>
        </NUpload>
        <NButton
          quaternary
          size="small"
          type="error"
          class="album-del-btn"
          @click="showDeleteAlbum = true"
          aria-label="删除相册"
        >
          <template #icon><Trash2 :size="14" :stroke-width="1.8" /></template>
          删除相册
        </NButton>
      </div>

      <!-- 封面 hero：View Transition 共享元素目标（列表封面放大进入详情） -->
      <div class="album-hero" v-if="currentAlbum">
        <div class="album-hero-cover" ref="detailCoverEl">
          <img v-if="currentAlbum.cover" :src="currentAlbum.cover" :alt="currentAlbum.albumName" class="detail-cover" />
          <div v-else class="album-cover-ph">{{ currentAlbum.albumName.slice(0, 1) }}</div>
        </div>
        <div class="album-hero-meta">
          <div class="album-hero-count">{{ images.length }} 张照片 · 与 TA 共享</div>
        </div>
      </div>

      <div class="album-toolbar">
        <NInput
          v-model:value="imgKeyword"
          placeholder="搜索照片（备注或文件名）"
          clearable
          size="small"
          aria-label="搜索照片"
          class="album-search"
        >
          <template #prefix><Search :size="15" :stroke-width="1.8" /></template>
        </NInput>
        <NButton
          quaternary
          size="small"
          :type="onlyFav ? 'primary' : 'default'"
          :class="{ on: onlyFav }"
          class="fav-toggle"
          @click="onlyFav = !onlyFav"
        >
          <template #icon><Heart :size="14" :stroke-width="1.8" :fill="onlyFav ? 'currentColor' : 'none'" /></template>
          仅看收藏
        </NButton>
        <NButton
          quaternary
          size="small"
          :type="selectMode ? 'primary' : 'default'"
          :class="{ on: selectMode }"
          class="sel-toggle"
          @click="toggleSelectMode"
        >
          <template #icon><CheckSquare :size="14" :stroke-width="1.8" /></template>
          选择
        </NButton>
        <NButton
          quaternary
          size="small"
          class="import-toggle"
          @click="openImport"
        >
          <template #icon><ImagePlus :size="14" :stroke-width="1.8" /></template>
          批量导入
        </NButton>
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
            <NButton type="primary" v-press-bounce>＋ 上传第一张照片</NButton>
          </NUpload>
        </template>
      </IndEmpty>

      <IndEmpty
        v-else-if="filteredImages.length === 0"
        title="没有匹配的照片"
        desc="换个关键词，或关闭「仅看收藏」试试"
      />

      <div v-else class="img-grid">
        <!-- 桌面端：网格首位的“添加照片”磁贴 -->
        <NUpload
          v-if="!isMobile() && !selectMode"
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

        <draggable
          v-model="images"
          item-key="id"
          class="img-drag"
          :handle="'.drag-handle'"
          :animation="160"
          :disabled="selectMode || isFiltering"
          @end="onReorder"
        >
          <template #item="{ element: img }">
            <div
              class="img-cell love-card"
              :class="{ selected: selectMode && selectedIds.has(img.id) }"
              v-show="matchesFilter(img)"
              role="button"
              tabindex="0"
              @click="onCellClick(img)"
              @keydown.enter.prevent="onCellClick(img)"
              @keydown.space.prevent="onCellClick(img)"
            >
              <img class="thumb" :src="img.url || img.imagePath" :alt="img.remark || 'photo'" loading="lazy" />
              <div class="img-cap" v-if="img.remark" v-show="!selectMode">{{ img.remark }}</div>
              <button class="img-fav" v-if="!selectMode" :class="{ on: favs.has(img.id) }" @click.stop="toggleFav(img)" :aria-label="favs.has(img.id) ? '取消收藏' : '收藏'"><Heart :size="14" :fill="favs.has(img.id) ? 'currentColor' : 'none'" /></button>
              <NButton v-if="!selectMode" class="img-del" size="tiny" quaternary circle :disabled="removingId === img.id" :aria-busy="removingId === img.id" :aria-label="removingId === img.id ? '正在删除' : '删除照片'" @click.stop="removeImage(img)">✕</NButton>
              <span class="drag-handle" v-if="!selectMode" @click.stop><GripVertical :size="16" :stroke-width="1.8" /></span>
              <div class="select-overlay" v-if="selectMode">
                <Check v-if="selectedIds.has(img.id)" :size="22" :stroke-width="3" />
              </div>
            </div>
          </template>
        </draggable>
      </div>

      <AlbumLightbox
        :images="lbImages"
        v-model="lightboxIndex"
        :favs="favs"
        @toggle-fav="onLightboxFav"
      />

      <!-- 批量操作栏（选择模式下） -->
      <div v-if="selectMode" class="batch-bar">
        <span class="batch-count">已选 {{ selectedIds.size }} 张</span>
        <NButton size="small" :disabled="!selectedIds.size" @click="openMove">移动到相册</NButton>
        <NButton size="small" type="error" :disabled="!selectedIds.size" @click="batchDelete">删除</NButton>
        <NButton size="small" quaternary @click="exitSelect">完成</NButton>
      </div>

      <NModal v-model:show="showMove" title="移动到相册" preset="card" style="width:92%;max-width:420px">
        <NSelect v-model:value="moveTarget" :options="moveOptions" placeholder="选择目标相册" />
        <template #footer>
          <div class="modal-foot">
            <NButton @click="showMove = false">取消</NButton>
            <NButton type="primary" :disabled="!moveTarget || !selectedIds.size" :loading="moving" @click="confirmBatchMove">移动</NButton>
          </div>
        </template>
      </NModal>

      <!-- #16-c 相册照片批量导入 -->
      <NModal v-model:show="showImport" title="批量导入照片到相册" preset="card" style="width:92%;max-width:460px" @after-leave="onImportClosed">
        <div class="import-body">
          <NFormItem label="目标相册" :show-feedback="false">
            <NSelect v-model:value="importTarget" :options="importOptions" placeholder="选择目标相册" />
          </NFormItem>
          <NUpload
            v-model:file-list="importFiles"
            multiple
            :max="20"
            accept="image/*"
            :show-file-list="true"
            list-type="image-card"
            class="import-upload"
          />
          <p class="import-hint">支持 jpg / png / gif / webp，单张 ≤ 5MB，最多 20 张；内容会自动校验并剥离位置信息。</p>
          <div v-if="importResult" class="import-result">
            <NTag :type="importResult.failed === 0 ? 'success' : 'warning'">
              成功 {{ importResult.imported }} 张 / 失败 {{ importResult.failed }} 张
            </NTag>
            <ul v-if="importResult.errors.length" class="import-errors">
              <li v-for="(e, i) in importResult.errors" :key="i">
                <strong>{{ e.fileName }}</strong>：{{ e.reason }}
              </li>
            </ul>
          </div>
        </div>
        <template #footer>
          <div class="modal-foot">
            <NButton @click="showImport = false">关闭</NButton>
            <NButton
              type="primary"
              :disabled="!importTarget || importFiles.length === 0"
              :loading="importing"
              @click="confirmBatchImport"
            >开始导入</NButton>
          </div>
        </template>
      </NModal>

      <!-- 删除相册确认 -->
      <NModal v-model:show="showDeleteAlbum" title="删除相册" preset="card" style="width:92%;max-width:420px">
        <p class="del-album-warn">
          确定删除「{{ currentAlbum?.albumName }}」吗？相册内的
          {{ currentAlbum?.imageCount ?? images.length }} 张照片会一并移入回收（对你和 TA 都不可见），且<strong>不可恢复</strong>。
        </p>
        <template #footer>
          <div class="modal-foot">
            <NButton @click="showDeleteAlbum = false">取消</NButton>
            <NButton type="error" :loading="deletingAlbum" @click="confirmDeleteAlbum">删除</NButton>
          </div>
        </template>
      </NModal>

      <!-- 移动端固定底部上传 -->
      <div v-if="isMobile()" class="upload-fab">
        <NUpload
          multiple
          :max="20"
          accept="image/*"
          :show-file-list="false"
          :custom-request="customRequest"
        >
          <NButton type="primary" block v-press-bounce>＋ 上传本地图片（最多 20 张）</NButton>
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
          <NButton v-press-bounce @click="showCreate = false">取消</NButton>
          <NButton type="primary" :loading="creating" v-press-bounce @click="createAlbum">创建</NButton>
        </div>
      </template>
    </NModal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted, nextTick } from 'vue';
import {
  NButton, NModal, NForm, NFormItem, NInput, NUpload, NTag, NSelect,
} from 'naive-ui';
import type { UploadCustomRequestOptions, UploadFileInfo } from 'naive-ui';
import type { AlbumDto, AlbumReq, ImageDto, ApiResult, PagedResult, AlbumImageBatchUploadResult } from '@/types';
import * as albumApi from '@/api/album';
import { isMobile } from '@/composables/useDevice';
import { useStaggerEnter } from '@/composables/useAnimation';
import { startSharedTransition } from '@/composables/useViewTransition';
import { useSettingStore } from '@/store/settingStore';
import AlbumLightbox from '@/components/album/AlbumLightbox.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndLed from '@/components/industrial/IndLed.vue';
import { feedback } from '@/utils/feedback';
import { Heart, Search, GripVertical, Check, CheckSquare, ImagePlus, Trash2 } from 'lucide-vue-next';
import { requiredRule } from '@/utils/formRules';
import ImageField from '@/components/Common/ImageField.vue';
import draggable from 'vuedraggable';

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
  // 先批量读取所有卡片位置（仅触发一次回流），再统一写入 transform，
  // 避免原「逐元素读写」导致的 layout thrashing（每次 getBoundingClientRect 强制同步回流）
  const cards = Array.from(grid.querySelectorAll<HTMLElement>('.album-cover'));
  const measures: { img: HTMLElement | null; center: number }[] = cards.map((card) => {
    const img = card.querySelector<HTMLElement>('img');
    const r = card.getBoundingClientRect();
    return { img, center: r.top + r.height / 2 };
  });
  for (const { img, center } of measures) {
    if (!img) continue;
    // 卡片中心相对视口中心的归一化偏移（-0.5 ~ 0.5），乘以系数得到 ±24px 视差
    const delta = (center - vh / 2) / vh;
    img.style.transform = `translateY(${(-delta * 24).toFixed(2)}px)`;
  }
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
const detailCoverEl = ref<HTMLElement | null>(null); // 详情封面 hero（View Transition 共享元素目标）

const showCreate = ref(false);
const showDeleteAlbum = ref(false);
const deletingAlbum = ref(false);
const creating = ref(false);
const form = reactive<AlbumReq>({ albumName: '', cover: '', remark: '' });
const favs = ref<Set<number>>(new Set());
function toggleFav(img: ImageDto) {
  const s = new Set(favs.value);
  s.has(img.id) ? s.delete(img.id) : s.add(img.id);
  favs.value = s;
}

// #21 移动端筛选：相册列表 + 相册内图片的搜索 / 收藏筛选（前端数组过滤，无额外请求）
const albumKeyword = ref('');
const imgKeyword = ref('');
const onlyFav = ref(false);

const filteredAlbums = computed(() => {
  const kw = albumKeyword.value.trim().toLowerCase();
  if (!kw) return albums.value;
  return albums.value.filter((a) => (a.albumName || '').toLowerCase().includes(kw));
});

const filteredImages = computed(() => {
  let r = images.value;
  if (onlyFav.value) r = r.filter((i) => favs.value.has(i.id));
  const kw = imgKeyword.value.trim().toLowerCase();
  if (kw) {
    r = r.filter((i) =>
      ((i.remark || '') + ' ' + (i.url || i.imagePath || '')).toLowerCase().includes(kw),
    );
  }
  return r;
});

const lightboxIndex = ref(-1);
const lbImages = computed(() =>
  filteredImages.value.map((i) => ({ id: i.id, url: i.url || i.imagePath, remark: i.remark }))
);
function openLightbox(img: ImageDto) {
  lightboxIndex.value = filteredImages.value.findIndex((i) => i.id === img.id);
}
function onLightboxFav(id: number) {
  const img = images.value.find((i) => i.id === id);
  if (img) toggleFav(img);
}

async function loadAlbums() {
  loading.value = true;
  try {
    // 相册数量通常很少，单次拉全量（避免超过 pageSize 被后端截断导致前端过滤漏项）
    const res = await albumApi.listAlbum({ page: 1, pageSize: 200 });
    albums.value = (res.data as ApiResult<{ items: AlbumDto[] }>).data?.items ?? [];
  } finally {
    loading.value = false;
  }
  // 必须在 loading=false（网格真实渲染）之后、再 nextTick，applyParallax 才能取到 .album-grid
  await nextTick();
  applyParallax();
}

async function openAlbum(a: AlbumDto, e?: Event) {
  // P2-13：列表封面 → 详情 hero 封面的共享元素放大过渡（不支持/reduce-motion 时自动降级为直切）
  const src = e
    ? (e.currentTarget as HTMLElement).querySelector<HTMLElement>('.album-cover img')
    : null;
  const swap = () => {
    currentAlbum.value = a;
    imgLoading.value = true;
    images.value = [];
  };
  await startSharedTransition({
    source: src,
    name: 'album-cover',
    applyTarget: () => detailCoverEl.value?.querySelector('img') ?? detailCoverEl.value,
    swap,
  });
  try {
    const res = await albumApi.listImages(a.id);
    images.value = (res.data as ApiResult<ImageDto[]>).data ?? [];
  } finally {
    imgLoading.value = false;
  }
}

function backToList() {
  const src = detailCoverEl.value?.querySelector('img') ?? null;
  const id = currentAlbum.value?.id;
  const swap = () => {
    currentAlbum.value = null;
    loadAlbums();
  };
  // 反向：详情 hero → 列表对应封面（列表异步重渲染，目标可能错过快照 → 自动降级淡入）
  void startSharedTransition({
    source: src,
    name: 'album-cover',
    applyTarget: () =>
      id
        ? (document.querySelector(`.album-grid [data-album-id="${id}"] .album-cover img`) as HTMLElement | null)
        : null,
    swap,
  });
}

async function confirmDeleteAlbum() {
  if (!currentAlbum.value) return;
  const id = currentAlbum.value.id;
  deletingAlbum.value = true;
  try {
    await albumApi.deleteAlbum(id);
    feedback.deleted('相册');
    showDeleteAlbum.value = false;
    currentAlbum.value = null;
    await loadAlbums();
  } catch {
    feedback.error('删除失败，请重试');
  } finally {
    deletingAlbum.value = false;
  }
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
    if (currentAlbum.value) currentAlbum.value.imageCount = (currentAlbum.value.imageCount || 0) + 1;
    feedback.saved('照片');
    opt.onFinish();
  } catch {
    opt.onError();
  }
}

const removingId = ref<number | null>(null);
async function removeImage(img: ImageDto) {
  if (removingId.value === img.id) return; // 防重复点击：同一张正在删时忽略
  removingId.value = img.id;
  try {
    await albumApi.deleteImage(img.id);
    images.value = images.value.filter((x) => x.id !== img.id);
    if (currentAlbum.value) currentAlbum.value.imageCount = Math.max(0, currentAlbum.value.imageCount - 1);
    feedback.deleted('照片');
  } catch {
    // 删除失败：不本地移除，避免 UI 与服务器不一致；重新拉取保证同步
    feedback.error('删除失败，请重试');
    if (currentAlbum.value) {
      const res = await albumApi.listImages(currentAlbum.value.id);
      images.value = (res.data as ApiResult<ImageDto[]>).data ?? [];
    }
  } finally {
    removingId.value = null;
  }
}

// #17 相册批量：多选 + 批量删除 / 移动到其他相册 + 拖拽排序
const selectMode = ref(false);
const selectedIds = ref<Set<number>>(new Set());
const showMove = ref(false);
const moveTarget = ref<number | null>(null);
const moving = ref(false);
const moveOptions = ref<{ label: string; value: number }[]>([]);

const isFiltering = computed(() => !!imgKeyword.value.trim() || onlyFav.value);
function matchesFilter(img: ImageDto) {
  if (onlyFav.value && !favs.value.has(img.id)) return false;
  const kw = imgKeyword.value.trim().toLowerCase();
  if (kw && !((img.remark || '') + ' ' + (img.url || img.imagePath || '')).toLowerCase().includes(kw)) return false;
  return true;
}
function toggleSelectMode() {
  selectMode.value = !selectMode.value;
  if (!selectMode.value) selectedIds.value = new Set();
}
function exitSelect() {
  selectMode.value = false;
  selectedIds.value = new Set();
}
function onCellClick(img: ImageDto) {
  if (selectMode.value) {
    const s = new Set(selectedIds.value);
    s.has(img.id) ? s.delete(img.id) : s.add(img.id);
    selectedIds.value = s;
  } else {
    openLightbox(img);
  }
}
async function onReorder() {
  try {
    await albumApi.reorderImages(images.value.map((i) => i.id));
  } catch {
    feedback.error('排序保存失败，已撤销');
    if (currentAlbum.value) {
      const res = await albumApi.listImages(currentAlbum.value.id);
      images.value = (res.data as ApiResult<ImageDto[]>).data ?? [];
    }
  }
}
async function batchDelete() {
  if (!selectedIds.value.size) return;
  const ids = [...selectedIds.value];
  try {
    await albumApi.batchDeleteImages(ids);
    images.value = images.value.filter((i) => !selectedIds.value.has(i.id));
    if (currentAlbum.value) currentAlbum.value.imageCount = Math.max(0, currentAlbum.value.imageCount - ids.length);
    feedback.deleted('所选照片');
  } catch {
    feedback.error('批量删除失败，请重试');
  } finally {
    exitSelect();
  }
}
async function openMove() {
  if (!selectedIds.value.size) return;
  try {
    const res = await albumApi.listAlbum({ page: 1, pageSize: 100 });
    const all = (res.data as ApiResult<PagedResult<AlbumDto>>).data?.items ?? [];
    moveOptions.value = all
      .filter((a) => !currentAlbum.value || a.id !== currentAlbum.value.id)
      .map((a) => ({ label: a.albumName, value: a.id }));
    moveTarget.value = null;
    showMove.value = true;
  } catch {
    feedback.error('获取相册列表失败');
  }
}
async function confirmBatchMove() {
  if (!moveTarget.value || !selectedIds.value.size) return;
  const ids = [...selectedIds.value];
  moving.value = true;
  try {
    await albumApi.batchMoveImages(ids, moveTarget.value);
    images.value = images.value.filter((i) => !selectedIds.value.has(i.id));
    if (currentAlbum.value) currentAlbum.value.imageCount = Math.max(0, currentAlbum.value.imageCount - ids.length);
    feedback.moved('所选照片');
    showMove.value = false;
  } catch {
    feedback.error('批量移动失败，请重试');
  } finally {
    moving.value = false;
    exitSelect();
  }
}

// #16-c 相册照片批量导入：选目标相册 + 多选文件，一次请求归库，自动刷新当前相册
const showImport = ref(false);
const importTarget = ref<number | null>(null);
const importOptions = ref<{ label: string; value: number }[]>([]);
const importFiles = ref<UploadFileInfo[]>([]);
const importing = ref(false);
const importResult = ref<AlbumImageBatchUploadResult | null>(null);

async function openImport() {
  try {
    const res = await albumApi.listAlbum({ page: 1, pageSize: 100 });
    const all = (res.data as ApiResult<PagedResult<AlbumDto>>).data?.items ?? [];
    importOptions.value = all.map((a) => ({ label: a.albumName, value: a.id }));
    importTarget.value = currentAlbum.value?.id ?? (all[0]?.id ?? null);
    importFiles.value = [];
    importResult.value = null;
    showImport.value = true;
  } catch {
    feedback.error('获取相册列表失败');
  }
}
async function confirmBatchImport() {
  const target = importTarget.value;
  if (!target) { feedback.error('请选择目标相册'); return; }
  const files = importFiles.value.map((f) => f.file as File).filter(Boolean);
  if (!files.length) { feedback.error('请先选择要导入的照片'); return; }
  importing.value = true;
  importResult.value = null;
  try {
    const res = await albumApi.batchUploadImages(target, files);
    const r = (res.data as ApiResult<AlbumImageBatchUploadResult>).data!;
    importResult.value = r;
    feedback.imported(r.imported, 0, r.failed);
    // 若导入到当前相册，刷新列表并刷新计数；否则仅累加计数
    if (currentAlbum.value && currentAlbum.value.id === importTarget.value) {
      const list = await albumApi.listImages(currentAlbum.value.id);
      images.value = (list.data as ApiResult<ImageDto[]>).data ?? [];
      currentAlbum.value.imageCount = images.value.length;
    } else if (currentAlbum.value) {
      currentAlbum.value.imageCount = Math.max(0, currentAlbum.value.imageCount + r.imported);
    }
  } catch {
    feedback.error('批量导入失败，请重试');
  } finally {
    importing.value = false;
  }
}
function onImportClosed() {
  // 关闭后清空结果与文件，避免下次打开残留
  importResult.value = null;
  importFiles.value = [];
}

import { useRealtime, overlaySyncMap } from '@/composables/useRealtime';
import { useSyncSettle } from '@/composables/useSyncSettle';
const { useModuleSync } = useRealtime();
onMounted(async () => {
  await loadAlbums();
  useModuleSync('album', { items: albums, getId: i => i.id, load: loadAlbums, map: overlaySyncMap });
  // 伴侣新建相册时，相册卡错落入场
  useSyncSettle('album', container, albums, '.album-card');
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
.brand {
  display: flex; align-items: center; gap: 14px; padding: 12px 16px; margin-bottom: 8px;
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  box-shadow: var(--shadow-card);
}
.brand-status {
  margin-left: auto; display: inline-flex; align-items: center; gap: 6px;
  font-size: 12px; font-weight: 500;
  color: var(--color-ink-2);
  padding: 4px 12px; border-radius: 999px;
  background: var(--color-surface-2); border: 1px solid var(--color-border);
}
.ind-label { font-family: var(--font-mono); font-weight: 500; letter-spacing: 0.1em; font-size: 13px; color: var(--color-ink); margin: 0; }
.page-head { display: flex; align-items: center; gap: 12px; margin-bottom: 16px; }
.page-head h1 { margin: 0; font-size: 22px; }
.page-head h2 { margin: 0; font-size: 18px; }
.album-title { flex: 1; }
.page-head-solo { justify-content: flex-end; margin-bottom: 12px; }

/* 缩略图/封面淡入：元素挂载时轻淡入（reduce-motion 下由全局规则收敛为瞬时）。
   用 animation 而非 @load 处理，避免缓存命中时 @load 不触发导致图片永久不可见。 */
@keyframes img-fade-in { from { opacity: 0; } to { opacity: 1; } }

.album-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(160px, 1fr)); gap: 14px; }
.album-card { padding: 0; overflow: hidden; cursor: pointer; transition: transform var(--dur-micro) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love); }
.album-cover { position: relative; aspect-ratio: 1 / 1; overflow: hidden; background: var(--color-ink-soft); }
.album-cover img { position: absolute; top: -15%; left: 0; width: 100%; height: 130%; object-fit: cover; transition: transform var(--dur-micro) var(--ease-love); animation: img-fade-in var(--dur-pop) var(--ease-love) both; }
.album-cover-ph { width: 100%; height: 100%; display: grid; place-items: center; color: var(--color-ink-3); }

/* 详情封面 hero（View Transition 共享元素目标：列表封面放大进入） */
.album-hero { display: flex; align-items: flex-start; gap: 16px; margin: 4px 0 14px; }
.album-hero-cover { width: 148px; flex-shrink: 0; }
.album-hero-cover .detail-cover,
.album-hero-cover .album-cover-ph {
  width: 148px; height: 148px; object-fit: cover; border-radius: var(--radius-md);
  box-shadow: var(--shadow-card); display: grid; place-items: center; font-size: 40px; color: var(--color-ink-3);
  background: var(--color-ink-soft);
}
.album-hero-meta { padding-top: 4px; }
.album-hero-count { font-size: 13px; color: var(--color-ink-3); }
/* 浮起改用分层景深令牌：暗色下 --elev-3 走深底阴影，修正原硬编码近黑阴影在暗色不可见的问题 */
html:not(.reduce-motion) .album-card:hover { transform: translateY(-3px) scale(1.015); box-shadow: var(--elev-3); }
.album-meta { padding: 10px 12px; }
.album-name { font-weight: 500; }

.img-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(108px, 1fr)); gap: 8px; }
.img-drag { display: contents; }
.img-cell { position: relative; padding: 0; overflow: hidden; aspect-ratio: 1; cursor: pointer; border-radius: var(--radius-md); transition: transform var(--dur-micro) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love); }
.img-cell :deep(img) { width: 100%; height: 100%; object-fit: cover; display: block; transition: transform var(--dur-micro) var(--ease-love); animation: img-fade-in var(--dur-pop) var(--ease-love) both; }
.thumb { width: 100%; height: 100%; object-fit: cover; display: block; cursor: zoom-in; transition: transform var(--dur-micro) var(--ease-love); animation: img-fade-in var(--dur-pop) var(--ease-love) both; }
html:not(.reduce-motion) .img-cell:hover :deep(img) { transform: scale(1.05); }
/* 批量选择态：高亮描边 + 轻微回缩，带平滑过渡（替代原来生硬 snap） */
.img-cell.selected { box-shadow: 0 0 0 2px var(--color-accent); transform: scale(0.97); }
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
.del-album-warn { margin: 0 0 4px; color: var(--color-ink-2); line-height: 1.6; font-size: 14px; }
.del-album-warn strong { color: var(--color-rose-text); }
.album-del-btn { margin-left: 2px; }
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
.add-tile-wrap { margin: 0; }
.drag-handle {
  position: absolute; right: 6px; bottom: 6px; width: 26px; height: 26px; border-radius: 6px;
  background: rgba(0, 0, 0, 0.45); color: #fff; display: grid; place-items: center; cursor: grab;
  opacity: 0; transition: opacity var(--dur-micro) var(--ease-love); z-index: 3;
}
html:not(.reduce-motion) .img-cell:hover .drag-handle, .drag-handle:active { opacity: 1; }
.drag-handle:active { cursor: grabbing; }
.select-overlay {
  position: absolute; inset: 0; display: grid; place-items: center; color: #fff;
  background: rgba(0, 0, 0, 0.38); border: 2px solid transparent; box-sizing: border-box; z-index: 2;
  transition: background var(--dur-pop) var(--ease-love), border-color var(--dur-pop) var(--ease-love);
}
.img-cell.selected .select-overlay { background: rgb(var(--color-rose-rgb, 255 111 125) / 0.28); border-color: var(--color-rose); }
.sel-toggle { flex: 0 0 auto; }
.batch-bar {
  position: fixed; left: 16px; right: 16px; bottom: calc(env(safe-area-inset-bottom) + 16px);
  z-index: 30; display: flex; align-items: center; gap: 10px; padding: 10px 14px;
  background: var(--color-surface, #fff); border-radius: var(--radius-lg); box-shadow: var(--elev-4);
}
.batch-count { flex: 1; font-size: 13px; color: var(--color-ink-2); }
.add-tile {
  width: 100%; aspect-ratio: 1 / 1; display: flex; flex-direction: column;
  align-items: center; justify-content: center; gap: 6px; cursor: pointer;
  border: 2px dashed var(--color-accent-soft); border-radius: var(--radius-md);
  color: var(--color-accent-text); background: var(--color-accent-soft);
  transition: transform var(--dur-micro) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love);
}
html:not(.reduce-motion) .add-tile:hover { transform: scale(1.03); box-shadow: var(--elev-3); }
.add-plus { font-size: 30px; line-height: 1; }
.add-txt { font-size: 13px; font-weight: 500; }
:deep(.add-tile-wrap .n-upload-trigger) { width: 100%; height: 100%; }
:deep(.head-upload) { margin-left: auto; }
.album-toolbar {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 16px;
  flex-wrap: wrap;
}
.album-search { flex: 1 1 220px; min-width: 0; }
.fav-toggle { flex: 0 0 auto; }
@media (max-width: 520px) {
  .album-toolbar { flex-direction: column; align-items: stretch; }
  .album-search { flex: 1 1 100%; }
  .fav-toggle { width: 100%; justify-content: center; }
}

@media (max-width: 767px) {
  :global(.album-modal) { width: 100vw !important; max-width: 100vw !important; height: 100dvh; margin: 0; border-radius: 0; }
  .album-grid { grid-template-columns: repeat(2, 1fr); }
  .img-grid { grid-template-columns: repeat(auto-fill, minmax(84px, 1fr)); }
  /* 移动端固定上传 / 批量栏会盖住最后一行，给内容区留底白 */
  .album-page { padding-bottom: 84px; }
  .brand { padding: 10px 14px; }
  .brand .ind-label { font-size: 12px; }
  .brand-status { padding: 3px 9px; font-size: 11px; }
}

/* #16-c 相册照片批量导入 */
.import-toggle { flex: 0 0 auto; }
.import-body { display: flex; flex-direction: column; gap: 12px; }
.import-upload { :deep(.n-upload-trigger), :deep(.n-upload-file-info__thumbnail) { border-radius: 8px; } }
.import-hint { margin: 0; font-size: 12px; color: var(--n-text-color-3, #9aa0a6); line-height: 1.5; }
.import-result { display: flex; flex-direction: column; gap: 8px; }
.import-errors { margin: 0; padding-left: 18px; font-size: 12px; color: #d97706; line-height: 1.6; }
.import-errors strong { color: #b45309; }
</style>
