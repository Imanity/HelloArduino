package thss.helloandroid;

import java.util.Iterator;
import java.util.Set;
import android.app.Activity;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.Button;

public class MainActivity extends Activity {
    private Button mybutton = null;
    public void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        //得到按钮
         mybutton = (Button)findViewById(R.id.btn2);
        //绑定监听器
        mybutton.setOnClickListener(new ButtonListener());
    }
    

    //监听器匿名类
    private class ButtonListener implements OnClickListener
    {
        public void onClick(View v)
        {
        //得到BluetoothAdapter对象
        BluetoothAdapter adapter = BluetoothAdapter.getDefaultAdapter();
        //判断BluetoothAdapter对象是否为空，如果为空，则表明本机没有蓝牙设备
        if(adapter != null)
        {
            System.out.println("本机拥有蓝牙设备");
            //调用isEnabled()方法判断当前蓝牙设备是否可用
            if(!adapter.isEnabled())
            {
                //如果蓝牙设备不可用的话,创建一个intent对象,该对象用于启动一个Activity,提示用户启动蓝牙适配器
                Intent intent = new Intent(BluetoothAdapter.ACTION_REQUEST_ENABLE);
                startActivity(intent);
                }
            //得到所有已经配对的蓝牙适配器对象
            Set<BluetoothDevice> devices = adapter.getBondedDevices();
            if(devices.size()>0)
            {
                //用迭代
                for(Iterator iterator = devices.iterator();iterator.hasNext();)
                {
                    //得到BluetoothDevice对象,也就是说得到配对的蓝牙适配器
                    BluetoothDevice device = (BluetoothDevice)iterator.next();
                    //得到远程蓝牙设备的地址
                    Log.d("mytag", device.getAddress());
                    }
                }
            } else {
                System.out.println("没有蓝牙设备");
            }
        }
    }
}
