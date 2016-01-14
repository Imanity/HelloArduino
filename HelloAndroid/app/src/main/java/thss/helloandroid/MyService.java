package thss.helloandroid;

import android.app.Service;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothSocket;
import android.content.Intent;
import android.os.IBinder;
import android.util.Log;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.util.Iterator;
import java.util.Set;
import java.util.UUID;

public class MyService extends Service {
    private static final UUID HC_UUID   = UUID.fromString("00001101-0000-1000-8000-00805F9B34FB");
    private BluetoothAdapter mBtAdapter = null;
    private BluetoothSocket mBtSocket   = null;
    private OutputStream outStream = null;
    private InputStream inStream  = null;
    private boolean mBtFlag = true;
    private boolean threadFlag = false;
    private MyThread mThread = null;

    @Override
    public void onStart(Intent intent, int startId) {
        myStartService();
    }

    private void myStartService() {
        mBtAdapter = BluetoothAdapter.getDefaultAdapter();
        if ( mBtAdapter == null ) {
            showToast("Bluetooth unused.");
            mBtFlag  = false;
            return;
        }
        if ( !mBtAdapter.isEnabled() ) {
            mBtFlag  = false;
            //myStopService();
            showToast("Open bluetooth then restart program!!");
            return;
        }

        showToast("Start searching!!");
        threadFlag = true;
        mThread = new MyThread();
        mThread.start();
    }

    public class MyThread extends Thread {
        @Override
        public void run() {
            super.run();
            myBtConnect();
            while( threadFlag ) {
                readSerial();
                try{
                    Thread.sleep(100);
                }catch(Exception e){
                    e.printStackTrace();
                }
            }
        }
    }

    public void myBtConnect() {
        showToast("Connecting...");

//  BluetoothDevice mBtDevice = mBtAdapter.getRemoteDevice(HC_MAC);
        BluetoothDevice mBtDevice = null;
        Set<BluetoothDevice> mBtDevices = mBtAdapter.getBondedDevices();
        if ( mBtDevices.size() > 0 ) {
            for ( Iterator<BluetoothDevice> iterator = mBtDevices.iterator();
                  iterator.hasNext(); ) {
                mBtDevice = (BluetoothDevice)iterator.next();
                showToast(mBtDevice.getName() + "|" + mBtDevice.getAddress());
            }
        }

        try {
            mBtSocket = mBtDevice.createRfcommSocketToServiceRecord(HC_UUID);
        } catch (IOException e) {
            e.printStackTrace();
            mBtFlag = false;
            showToast("Create bluetooth socket error");
        }

        mBtAdapter.cancelDiscovery();

    /* Setup connection */
        try {
            mBtSocket.connect();
            showToast("Connect bluetooth success");
            //Log.i(TAG, "Connect " + HC_MAC + " Success!");
        } catch (IOException e) {
            e.printStackTrace();
            try {
                showToast("Connect error, close");
                mBtSocket.close();
                mBtFlag = false;
            } catch (IOException e1) {
                e1.printStackTrace();
            }
        }

    /* I/O initialize */
        if ( mBtFlag ) {
            try {
                inStream  = mBtSocket.getInputStream();
                outStream = mBtSocket.getOutputStream();
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
        showToast("Bluetooth is ready!");
    }

    public int readSerial() {
        int ret = 0;
        byte[] rsp = null;

        if ( !mBtFlag ) {
            return -1;
        }
        try {
            rsp = new byte[inStream.available()];
            ret = inStream.read(rsp);
            showToast(new String(rsp));
        } catch (IOException e) {
            // TODO Auto-generated catch block
            e.printStackTrace();
        }
        return ret;
    }

    public void writeSerial(int value) {
        String ha = "" + value;
        try {
            outStream.write(ha.getBytes());
            outStream.flush();
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    public void showToast(String str) {
        Intent intent = new Intent();
        //intent.putExtra("cmd", CMD_SHOW_TOAST);
        intent.putExtra("str", str);
        intent.setAction("outputAction");
        sendBroadcast(intent);
    }

    @Override
    public IBinder onBind(Intent intent) {
        // TODO: Return the communication channel to the service.
        throw new UnsupportedOperationException("Not yet implemented");
    }
}
