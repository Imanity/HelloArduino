package thss.helloandroid;

import android.app.Activity;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.graphics.Canvas;
import android.graphics.Paint;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.TextView;

public class MainActivity extends Activity {
    private double x = 0, y = 0, z = 0;
    MyView view;

    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        view = new MyView(this);
        setContentView(view);
        //接收广播
        Intent i = new Intent(MainActivity.this, MyService.class);
        startService(i);
        IntentFilter intentFilter = new IntentFilter("outputAction");
        MyBroadCastReceiver myBroadCastReceiver = new MyBroadCastReceiver();
        registerReceiver(myBroadCastReceiver, intentFilter);
    }

    //获得并解析蓝牙传输信息
    private class MyBroadCastReceiver extends BroadcastReceiver {
        @Override
        public void onReceive(Context context, Intent intent) {
            Bundle myBundle = intent.getExtras();
            String info = myBundle.getString("str");
            if (info.indexOf('<') == -1 || info.indexOf('>') == -1) { //接收信息不符合数据格式
                //myTextView.setText(info);
            } else {
                String detail = info.substring(info.indexOf('<'), info.lastIndexOf('>'));
                String data[] = detail.split("<");
                for (int i = 0; i < data.length; ++i) {
                    if (data[i].length() == 0)
                        continue;
                    data[i] = data[i].substring(0, data[i].length() - 1);
                    String key[] = data[i].split(":");
                    if (key[0].length() == 1) {
                        switch (key[0].charAt(0)) {
                            case 'x': x = Double.valueOf(key[1]); break;
                            case 'y': y = Double.valueOf(key[1]); break;
                            case 'z': z = Double.valueOf(key[1]); break;
                            default: break;
                        }
                    }
                }
            }
            view.postInvalidate();
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
            paint.setTextSize(25);
            canvas.drawText("x:" + x + "\ty:" + y + "\tz:" + z, 20, 20, paint);
            super.onDraw(canvas);
        }
    }
}
