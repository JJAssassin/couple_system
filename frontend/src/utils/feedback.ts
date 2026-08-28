import { useNotifyStore } from '@/store/notifyStore';

/**
 * 统一的反馈文案与入口。
 * 所有成功 / 失败 / 提示都走这里，保证措辞一致、语气贴合「双人恋爱」调性，
 * 不再散落各种随手写的字符串。
 */
const n = () => useNotifyStore();

export const feedback = {
  // —— 成功类 ——
  saved: (what = '') => n().success(`已保存${what}`),
  created: (what = '') => n().success(`已添加${what}`),
  updated: (what = '') => n().success(`已更新${what}`),
  deleted: (what = '') => n().success(`已删除${what}`),
  moved: (what = '') => n().success(`已移动${what}`),
  sended: (what = '') => n().success(`已发送${what}`),
  bound: () => n().success('绑定成功，你们的数据现在双向同步啦'),
  unbound: () => n().success('已解除绑定'),
  exported: (name = '') => n().success(`已开始下载：${name}`),
  copied: (what = '已复制') => n().success(what),

  // —— 信息 / 提示类 ——
  info: (m: string) => n().info(m),
  warn: (m: string) => n().info(m),

  // —— 错误类 ——
  error: (m = '操作失败，请稍后再试') => n().error(m),
  network: () => n().error('网络异常：请确认后端服务已启动（dotnet run）'),
  forbidden: () => n().error('无权访问该内容'),
  needPartner: () => n().info('先去「设置」绑定对方，才能一起记录哦'),
};
