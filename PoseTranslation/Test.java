public class Test{
    static Translator t;
    public static void main(String args[]){
        Translator t = new Translator();
        System.out.println("(roll, pitch, yaw) -> (x, y, z)");
        double pitch_arr[] = {-45, 0, 60};
        double yaw_arr[] = {-75, 0, 135};
        for(double yaw : yaw_arr)
            for(double pitch : pitch_arr)
                do_test(pitch, yaw);

    }
    static void do_test(double pitch, double yaw){
        Vector3D v= new Vector3D(0, pitch, yaw); //roll doesn't matter
        System.out.println(v.stringify() + " -> " + t.rpy_to_xyz(v).stringify());
    }
}
