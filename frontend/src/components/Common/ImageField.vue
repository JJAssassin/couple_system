<template>
  <div class="img-field">
    <div class="img-thumb" :style="boxStyle">
      <img v-if="modelValue" :src="assetUrl(modelValue)" :alt="label || '图片'" class="img-img" loading="lazy" />
      <div v-else class="img-ph">
        <ImageIcon :size="22" :stroke-width="1.6" />
      </div>
      <div v-if="uploading" class="img-mask"><n-spin size="small" /></div>
    </div>

    <div class="img-side">
      <n-upload
        accept="image/*"
        :show-file-list="false"
        :custom-request="onUpload"
        :disabled="uploading"
      >
        <n-button size="small" type="primary" secondary :loading="uploading">
          {{ modelValue ? '重新上传' : '选择图片' }}
        </n-button>
      </n-upload>
      <n-button v-if="modelValue" size="small" tertiary type="error" @click="clear">移除</n-button>
      <p class="img-hint">本地 jpg/png/gif/webp，≤25MB</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { NUpload, NButton, NSpin } from 'naive-ui';
import type { UploadCustomRequestOptions } from 'naive-ui';
import { Image as ImageIcon } from 'lucide-vue-next';
import { uploadStandalone } from '@/api/upload';
import { assetUrl } from '@/config/server';

const props = withDefaults(
  defineProps<{ modelValue?: string; label?: string; size?: number }>(),
  { modelValue: '', label: '', size: 84 },
);
const emit = defineEmits<{ 'update:modelValue': [string] }>();

const uploading = ref(false);
const boxStyle = computed(() => ({ width: `${props.size}px`, height: `${props.size}px` }));

async function onUpload(opt: UploadCustomRequestOptions) {
  const file = opt.file.file;
  if (!file) {
    opt.onError();
    return;
  }
  uploading.value = true;
  try {
    const path = await uploadStandalone(file);
    if (path) emit('update:modelValue', path);
    opt.onFinish();
  } catch {
    opt.onError();
  } finally {
    uploading.value = false;
  }
}

function clear() {
  emit('update:modelValue', '');
}
</script>

<style scoped>
.img-field { display: flex; align-items: center; gap: 14px; }
.img-thumb {
  position: relative; flex: 0 0 auto; border-radius: var(--radius-md, 10px);
  overflow: hidden; background: var(--color-surface-2);
  border: 1px solid var(--color-border);
}
.img-img { width: 100%; height: 100%; object-fit: cover; display: block; }
.img-ph {
  width: 100%; height: 100%; display: grid; place-items: center;
  color: var(--color-ink-3); background: var(--color-ink-soft);
}
.img-mask {
  position: absolute; inset: 0; display: grid; place-items: center;
  background: color-mix(in srgb, var(--color-surface) 55%, transparent);
}
.img-side { display: flex; flex-direction: column; align-items: flex-start; gap: 8px; }
.img-hint { margin: 0; font-size: 12px; color: var(--color-ink-3); }
</style>
