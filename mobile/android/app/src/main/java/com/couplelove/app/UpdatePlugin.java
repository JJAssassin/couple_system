package com.couplelove.app;

import android.app.DownloadManager;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.net.Uri;
import android.util.Log;

import androidx.core.content.FileProvider;

import androidx.core.content.FileProvider;

import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;

import java.io.File;

/**
 * 自托管 APK 自动更新（不依赖 Google Play）：
 * - getVersionCode(): 返回当前应用 versionCode，供 Web 端与云端 version.json 对比
 * - downloadAndInstall({url}): 用系统 DownloadManager 下载 APK 到应用外部目录，
 *   完成后经 FileProvider 拉起系统安装器（Android 8+ 需 REQUEST_INSTALL_PACKAGES 权限，
 *   首次安装会弹「允许安装未知应用」）。
 * Web 端通过 window.Capacitor.Plugins.Update 调用（Capacitor 自动暴露）。
 */
@CapacitorPlugin(name = "Update")
public class UpdatePlugin extends Plugin {

    private static final String TAG = "UpdatePlugin";

    @PluginMethod
    public void getVersionCode(PluginCall call) {
        try {
            // AGP 8 默认不生成 BuildConfig，改用 PackageManager 运行时读取（API 28+ 用 longVersionCode）
            int code = (int) getContext()
                    .getPackageManager()
                    .getPackageInfo(getContext().getPackageName(), 0)
                    .getLongVersionCode();
            JSObject ret = new JSObject();
            ret.put("versionCode", code);
            call.resolve(ret);
        } catch (Exception e) {
            call.reject(e.getMessage());
        }
    }

    @PluginMethod
    public void downloadAndInstall(PluginCall call) {
        String url = call.getString("url");
        if (url == null || url.isEmpty()) {
            call.reject("url required");
            return;
        }
        try {
            final Context ctx = getContext();
            DownloadManager dm = (DownloadManager) ctx.getSystemService(Context.DOWNLOAD_SERVICE);
            if (dm == null) {
                call.reject("DownloadManager unavailable");
                return;
            }
            final File dest = new File(ctx.getExternalFilesDir(null), "couple-update.apk");
            DownloadManager.Request req = new DownloadManager.Request(Uri.parse(url));
            req.setTitle("我们的小世界 更新");
            req.setDescription("正在下载新版本，请稍候…");
            req.setNotificationVisibility(DownloadManager.Request.VISIBILITY_VISIBLE_NOTIFY_COMPLETED);
            req.setDestinationUri(Uri.fromFile(dest));
            final long dlId = dm.enqueue(req);

            // 下载完成广播 → 安装（一次性；接收后自注销）
            ctx.registerReceiver(new BroadcastReceiver() {
                @Override
                public void onReceive(Context c, Intent intent) {
                    if (!DownloadManager.ACTION_DOWNLOAD_COMPLETE.equals(intent.getAction())) return;
                    long id = intent.getLongExtra(DownloadManager.EXTRA_DOWNLOAD_ID, -1);
                    if (id != dlId) return;
                    ctx.unregisterReceiver(this);
                    installApk(ctx, dest);
                }
            }, new IntentFilter(DownloadManager.ACTION_DOWNLOAD_COMPLETE));

            call.resolve();
        } catch (Exception e) {
            Log.e(TAG, "download fail", e);
            call.reject(e.getMessage());
        }
    }

    private void installApk(Context ctx, File apk) {
        try {
            Uri uri = FileProvider.getUriForFile(ctx, ctx.getPackageName() + ".fileprovider", apk);
            Intent intent = new Intent(Intent.ACTION_VIEW);
            intent.setDataAndType(uri, "application/vnd.android.package-archive");
            intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            ctx.startActivity(intent);
        } catch (Exception e) {
            Log.e(TAG, "install fail", e);
        }
    }
}
