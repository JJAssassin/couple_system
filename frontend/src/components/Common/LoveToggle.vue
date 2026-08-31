<template>
  <label class="uvi-toggle" :class="{ on: modelValue, disabled }">
    <input
      type="checkbox"
      role="switch"
      :aria-checked="modelValue"
      :aria-label="label"
      :checked="modelValue"
      :disabled="disabled"
      @change="onChange"
    />
    <span class="uvi-toggle__track"><span class="uvi-toggle__knob" /></span>
  </label>
</template>

<script setup lang="ts">
withDefaults(
  defineProps<{
    modelValue: boolean;
    disabled?: boolean;
    label?: string;
  }>(),
  { disabled: false }
);
const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void }>();
function onChange(e: Event) {
  emit('update:modelValue', (e.target as HTMLInputElement).checked);
}
</script>
