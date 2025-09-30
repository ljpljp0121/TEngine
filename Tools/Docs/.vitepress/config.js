import { defineConfig } from 'vitepress'
import { readdirSync, statSync } from 'fs'
import { join, dirname } from 'path'
import { fileURLToPath } from 'url'

const __dirname = dirname(fileURLToPath(import.meta.url))

// 动态生成侧边栏
function generateSidebar() {
  const staticDir = join(__dirname, '../static')
  const categories = ['Table', 'Bean', 'Enum']

  const sidebar = [
    {
      text: '总览',
      items: [
        { text: '总表', link: '/static/@Tables.md' }
      ]
    }
  ]

  categories.forEach(category => {
    const categoryPath = join(staticDir, category)
    if (!statSync(categoryPath, { throwIfNoEntry: false })) return

    const categoryItem = {
      text: `${category}`,
      collapsed: false,
      items: []
    }

    // 读取分类下的所有子目录（模块）
    const modules = readdirSync(categoryPath, { withFileTypes: true })
      .filter(dirent => dirent.isDirectory())
      .map(dirent => dirent.name)
      .sort()

    modules.forEach(module => {
      const modulePath = join(categoryPath, module)
      const files = readdirSync(modulePath)
        .filter(f => f.endsWith('.md'))
        .sort()

      if (files.length > 0) {
        const moduleItem = {
          text: module.charAt(0).toUpperCase() + module.slice(1),
          collapsed: true,
          items: files.map(file => {
            const name = file.replace('@', '').replace('.md', '')
            return {
              text: name,
              link: `/static/${category}/${module}/${file}`
            }
          })
        }
        categoryItem.items.push(moduleItem)
      }
    })

    // 处理分类根目录下的文件（如 结构/@vector2.md）
    const rootFiles = readdirSync(categoryPath)
      .filter(f => f.endsWith('.md'))
      .sort()

    if (rootFiles.length > 0) {
      const builtinItem = {
        text: 'Built-in Types',
        collapsed: true,
        items: rootFiles.map(file => {
          const name = file.replace('@', '').replace('.md', '')
          return {
            text: name,
            link: `/static/${category}/${file}`
          }
        })
      }
      categoryItem.items.unshift(builtinItem)
    }

    if (categoryItem.items.length > 0) {
      sidebar.push(categoryItem)
    }
  })

  return sidebar
}

export default defineConfig({
  title: '配置表文档',
  description: '项目文档网站',
  base: process.env.PUBLIC_URL || '/', // 使用环境变量设置base路径
  ignoreDeadLinks: true,

  vite: {
    server: {
      port: 5173,
      host: 'localhost'
    }
  },

  themeConfig: {
    nav: [
      // { text: '首页', link: '/' },
      { text: '指南', link: '/static/@Tables.md' }
    ],

    sidebar: generateSidebar(),

    socialLinks: [
      { icon: 'gitlab', link: 'http://10.21.216.120/ac25/doc/doc' }
    ],

    // 启用本地搜索功能
    search: {
      provider: 'local',
      options: {
        translations: {
          button: {
            buttonText: '搜索文档',
            buttonAriaLabel: '搜索文档'
          },
          modal: {
            noResultsText: '无法找到相关结果',
            resetButtonTitle: '清除查询条件',
            footer: {
              selectText: '选择',
              navigateText: '切换',
              closeText: '关闭'
            }
          }
        }
      }
    }
  }
})