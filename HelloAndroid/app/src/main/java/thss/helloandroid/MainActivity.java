package thss.helloandroid;

import android.app.Activity;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.os.Bundle;
import android.util.Log;
import android.view.Display;
import android.view.View;
import android.view.WindowManager;
import android.widget.Toast;

public class MainActivity extends Activity {
    private Vector3D[] rollPitchYaw;
    private Vector3D[] vector;
    private Vector3D[] nowVector;
    private int screenWidth = 0, screenHeight = 0;

    private MyView view;

    private boolean threadFlag = false;
    private MyThread mThread = null;

    public void showToast(String str){
        Toast.makeText(this, str, Toast.LENGTH_SHORT).show();
    }

    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        view = new MyView(this);
        view.setBackgroundColor(Color.WHITE);
        setContentView(view);

        //成员变量初始化
        vector = new Vector3D[5];
        nowVector = new Vector3D[5];
        rollPitchYaw = new Vector3D[5];
        for (int i = 0; i < 5; ++i) {
            vector[i] = new Vector3D(0, 0, 0);
        }
        for (int i = 0; i < 5; ++i) {
            nowVector[i] = new Vector3D(0, 0, 0);
        }
        for (int i = 0; i < 5; ++i) {
            rollPitchYaw[i] = new Vector3D(0, 0, 0);
        }

        initData();

        //获取屏幕分辨率
        WindowManager windowManager = getWindowManager();
        Display display = windowManager.getDefaultDisplay();
        screenWidth = display.getWidth();
        screenHeight = display.getHeight();

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

    //多线程刷新视图
    public class MyThread extends Thread {
        @Override
        public void run() {
            super.run();
            while( threadFlag ) {
                for (int i = 0; i < 5; ++i) {
                    nowVector[i].x += (vector[i].x - nowVector[i].x) / 4;
                    nowVector[i].y += (vector[i].y - nowVector[i].y) / 4;
                    nowVector[i].z += (vector[i].z - nowVector[i].z) / 4;
                }
                view.postInvalidate();
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

            /*if(parseChunk(info) > 0) {
                //更新视图
                view.postInvalidate();
            }*/
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

                for(int j = 0; j < 5; j++)
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


    //绘图类
    class MyView extends View {
        Paint paint = null;
        public MyView(Context context) {
            super(context);
            paint = new Paint();
        }
        public void onDraw(Canvas canvas) {
            //输出文字信息
            paint.setTextSize(25);
            paint.setStrokeWidth(3);
            /*
            canvas.drawText("rpy0" + rollPitchYaw[1].stringify(), 20, 20, paint);
            canvas.drawText("rpy1" + rollPitchYaw[3].stringify(), 20, 40, paint);
            canvas.drawText("xyz0" + vector[1].stringify(), 20, 60, paint);
            canvas.drawText("xyz1" + vector[3].stringify(), 20, 80, paint);
            */
            super.onDraw(canvas);
            //绘制火柴人
            float centerX = screenWidth / 2;
            float centerY = screenHeight / 2;
            float singleLength = screenWidth / 5;
            //头部
            canvas.drawCircle(centerX, centerY - 30, 30, paint);
            //躯干
            canvas.drawLine(centerX, centerY, centerX + (float) nowVector[0].x * singleLength * 2, centerY - (float) nowVector[0].z * singleLength * 2, paint);
            //大臂
            float tmpX1 = centerX + (float)nowVector[1].x * singleLength;
            float tmpX2 = centerX + (float)nowVector[2].x * singleLength;
            float tmpY1 = centerY - (float)nowVector[1].z * singleLength;
            float tmpY2 = centerY - (float)nowVector[2].z * singleLength;
            canvas.drawLine(centerX, centerY, tmpX1, tmpY1, paint);
            canvas.drawLine(centerX, centerY, tmpX2, tmpY2, paint);
            //小臂
            canvas.drawLine(tmpX1, tmpY1, tmpX1 + (float)nowVector[3].x * singleLength, tmpY1 - (float)nowVector[3].z * singleLength, paint);
            canvas.drawLine(tmpX2, tmpY2, tmpX2 + (float)nowVector[4].x * singleLength, tmpY2 - (float)nowVector[4].z * singleLength, paint);
        }
    }

    //For init
    private void initData() {
        //躯干
        nowVector[0].x = vector[0].x = 0;
        nowVector[0].y = vector[0].y = 0;
        nowVector[0].z = vector[0].z = -1;
        //左大臂
        nowVector[1].x = vector[1].x = 1;
        nowVector[1].y = vector[1].y = 0;
        nowVector[1].z = vector[1].z = 0;
        //右大臂
        nowVector[2].x = vector[2].x = -1;
        nowVector[2].y = vector[2].y = 0;
        nowVector[2].z = vector[2].z = 0;
        //左小臂
        nowVector[3].x = vector[3].x = 0;
        nowVector[3].y = vector[3].y = 0;
        nowVector[3].z = vector[3].z = -1;
        //右小臂
        nowVector[3].x = vector[4].x = 0;
        nowVector[3].y = vector[4].y = 0;
        nowVector[3].z = vector[4].z = 1;
    }
}
