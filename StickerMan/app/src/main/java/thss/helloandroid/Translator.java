package thss.helloandroid;

public class Translator {
    public static double degreeToRadian = 0.01745329251994329576923690768489; // Pi / 180;

    public static Vector3D rpy_to_xyz(Vector3D rollPitchYaw){ //in: degrees
        double roll = rollPitchYaw.x;
        double pitch = rollPitchYaw.y;
        double yaw = rollPitchYaw.z;
        double x, y, z;

        //天顶角：由俯仰角计算
        double theta = degreeToRadian;
        if(theta > 0)
            theta *= (90 - pitch);
        else
            theta *= (90 + (-pitch));

        //方位角：直接用偏航角
        double phi = yaw * degreeToRadian;

        //由球坐标转为直角坐标
        x = Math.sin(theta) * Math.cos(phi);
        y = Math.sin(theta) * Math.sin(phi);
        z = Math.cos(theta);
        return new Vector3D(x, y, z);
    }
}
