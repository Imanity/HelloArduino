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
    private Vector3D[] nowVector;

    //private MyView view;

    private boolean threadFlag = false;
    private MyThread mThread = null;

    public void showToast(String str){
        Toast.makeText(this, str, Toast.LENGTH_SHORT).show();
    }

    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        //成员变量初始化
        vector = new Vector3D[4];
        nowVector = new Vector3D[4];
        rollPitchYaw = new Vector3D[4];
        for (int i = 0; i < 4; ++i) {
            vector[i] = new Vector3D(0, 0, 0);
        }
        for (int i = 0; i < 4; ++i) {
            nowVector[i] = new Vector3D(0, 0, 0);
        }
        for (int i = 0; i < 4; ++i) {
            rollPitchYaw[i] = new Vector3D(0, 0, 0);
        }

        //接收广播
        Intent i = new Intent(MainActivity.this, MyService.class);
        startService(i);
        registerReceiver(new BluetoothChunkReceiver(), new IntentFilter("bluetoothChunk"));
        registerReceiver(new BluetoothToastReceiver(), new IntentFilter("bluetoothToast"));

        //开始多线程刷新绘图
        threadFlag = true;
        mThread = new MyThread();
        mThread.start();
    }

    //向unity输出
    public String message() {
        String str = "";
        for (int i = 0; i < 4; ++i) {
            str += nowVector[i].x + "," + nowVector[i].y + "," + nowVector[i].z + ",";
        }
        return str;
    }

    //多线程刷新视图
    public class MyThread extends Thread {
        @Override
        public void run() {
            super.run();
            while( threadFlag ) {
                for (int i = 0; i < 4; ++i) {
                    nowVector[i].x += (vector[i].x - nowVector[i].x) / 4;
                    nowVector[i].y += (vector[i].y - nowVector[i].y) / 4;
                    nowVector[i].z += (vector[i].z - nowVector[i].z) / 4;
                    double length = Math.sqrt(nowVector[i].x * nowVector[i].x + nowVector[i].y * nowVector[i].y + nowVector[i].z * nowVector[i].z);
                    if (length != 0) {
                        nowVector[i].x = nowVector[i].x / length;
                        nowVector[i].y = nowVector[i].y / length;
                        nowVector[i].z = nowVector[i].z / length;
                    }
                }
                //view.postInvalidate();
                try{
                    Thread.sleep(20);
                }catch(Exception e){
                    e.printStackTrace();
                }
            }
        }
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

        private String lastChunk = ""; //上一个数据块中最后一个 '>' 之后的部分

        //解析数据块 `><x:2.33><y:6.66><z:`，返回成功解析的数据个数
        public int parseChunk(String chunk){
            try {
                chunk = lastChunk + chunk;

                int left = chunk.indexOf('<');
                int right = chunk.lastIndexOf('>');

                if (left == -1) {
                    if(right == -1)
                        lastChunk = "";
                    else
                        lastChunk = chunk.substring(right + 1);
                    Log.w(LOG_TAG, chunk + " INVALID");

                    return 0;
                } else {
                    String numbers = chunk.substring(left, right + 1);
                    lastChunk = chunk.substring(right + 1);
                    return parseNumbers(numbers);
                }
            } catch (Exception e) {
                Log.e(LOG_TAG, chunk + " EXCEPTION");
                e.printStackTrace();
                return 0;
            }
        }

        //解析多个数字 `<x:2.33><y:6.66>`
        public int parseNumbers(String numbers){
            String data[] = numbers.split("<");
            int sum=0;
            for (int i = 0; i < data.length; ++i) {
                if (data[i].length() == 0)
                    continue;
                sum += parseNumber(data[i]);
            }
            return sum;
        }

        //解析单个数字 `x:2.33>`
        public int parseNumber(String number){
            number = number.substring(0, number.indexOf('>'));
            String key[] = number.split(":");
            if (key[0].length() == 1) {
                char chr = key[0].charAt(0);

                int i = ('z' - chr) / 3;
                /*
                x,y,z -> 0
                u,v,w -> 1
                r,s,t -> 2
                ...
                 */

                switch (chr % 3) {
                    case 0: rollPitchYaw[i].x = Double.valueOf(key[1]); break;
                    case 1: rollPitchYaw[i].y = Double.valueOf(key[1]); break;
                    case 2: rollPitchYaw[i].z = Double.valueOf(key[1]); break;
                    default: return 0;
                }

                for(int j = 0; j < 4; j++)
                    vector[j] = Translator.rpy_to_xyz(rollPitchYaw[j]);
                return 1;
            }
            return 0;
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
