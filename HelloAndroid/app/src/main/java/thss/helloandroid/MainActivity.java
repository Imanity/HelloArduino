package thss.helloandroid;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.os.Bundle;
import android.util.Log;
import android.widget.Toast;

import com.unity3d.player.UnityPlayerActivity;

public class MainActivity extends UnityPlayerActivity {
    private Vector3D[] rollPitchYaw;
    private Vector3D[] vector;

    //private MyView view;

    public void showToast(String str){
        Toast.makeText(this, str, Toast.LENGTH_SHORT).show();
    }

    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        //成员变量初始化
        vector = new Vector3D[4];
        rollPitchYaw = new Vector3D[4];
        for (int i = 0; i < 4; ++i) {
            vector[i] = new Vector3D(0, 0, 0);
        }
        for (int i = 0; i < 4; ++i) {
            rollPitchYaw[i] = new Vector3D(0, 0, 0);
        }

        //接收广播
        Intent i = new Intent(MainActivity.this, MyService.class);
        startService(i);
        registerReceiver(new BluetoothChunkReceiver(), new IntentFilter("bluetoothChunk"));
        registerReceiver(new BluetoothToastReceiver(), new IntentFilter("bluetoothToast"));
    }

    //向unity输出
    public String message() {
        String str = "";
        for (int i = 0; i < 4; ++i) {
            str += vector[i].x + "," + vector[i].y + "," + vector[i].z + ",";
        }
        return str;
    }

    //获得并解析蓝牙传输信息
    private class BluetoothChunkReceiver extends BroadcastReceiver {

        private String LOG_TAG = "bluetooth chunk";

        @Override
        public void onReceive(Context context, Intent intent) {
            Bundle myBundle = intent.getExtras();
            String info = myBundle.getString("str");
            Log.v(LOG_TAG, info);
            parseChunk(info);
        }
        public int parseChunk(String chunk) {
            char ch;
            char lower = 0;
            boolean lowerAssigned = false;
            for (int i = 0; i < chunk.length(); ++i) {
                ch = chunk.charAt(i);
                if ((ch & (1 << 6)) != 0) { // 01000000 as mask
                    if (!lowerAssigned)
                        continue;
                    char higher = (char)(ch & 3); // 00000011 as mask
                    char id = (char)((ch & 60) >> 2); // 00111100 as mask
                    int value = higher * 64 + lower;
                    Log.v(LOG_TAG, "BIN: " + Integer.toBinaryString((int)ch));
                    boolean isOutOfBound = false;
                    switch (id % 3) {
                        case 0:
                            rollPitchYaw[id / 3].x = value * 1.5 - 180;
                            break;
                        case 1:
                            rollPitchYaw[id / 3].y = value * 1.5 - 180;
                            break;
                        case 2:
                            rollPitchYaw[id / 3].z = value * 1.5 - 180;
                            break;
                        default:
                            isOutOfBound = true;
                            break;
                    }
                    if (isOutOfBound) {
                        Log.e(LOG_TAG, "Message Err: " + Integer.toBinaryString((int)ch));
                        continue;
                    }
                    lowerAssigned = false;
                } else {
                    lower = ch;
                    lowerAssigned = true;
                }
            }
            for(int i = 0; i < 4; i++)
                vector[i] = Translator.rpy_to_xyz(rollPitchYaw[i]);
            return 1;
        }        
    }

    private class BluetoothToastReceiver extends BroadcastReceiver {
        @Override
        public void onReceive(Context context, Intent intent) {
            Bundle myBundle = intent.getExtras();
            String info = myBundle.getString("str");
            Log.v("bluetooth toast", info);
            showToast(info);
        }
    }
}
