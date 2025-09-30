---
title: 表格文档
---

<script setup>
import { onMounted } from 'vue'

onMounted(() => {
  // 自动重定向到 Tables 页面
  if (typeof window !== 'undefined') {
    window.location.replace('static/@Tables.html')
  }
})
</script>

# 正在跳转到表格文档...

如果没有自动跳转，请点击 [这里](static/@Tables.html) 手动访问表格文档。

<style>
/* 隐藏页面内容，因为会立即重定向 */
.theme-default-content {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 50vh;
  text-align: center;
}
</style>
