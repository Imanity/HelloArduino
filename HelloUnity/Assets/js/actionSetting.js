//By 唐人杰 on 2016-1-26

#pragma strict

//人物部位编号
/*
0 - 上半身
1 - 左大臂
2 - 左小臂
3 - 右大臂
4 - 右小臂
*/
var id = 0;

//人物部件初始角度
var X_Offset : float;
var Y_Offset : float;
var Z_Offset : float;

//上一状态单位向量
var last_X = new float[4];
var last_Y = new float[4];
var last_Z = new float[4];

//肢体方向数据
var vectorArray = new float[12];
var D = 3;

//Android通信相关
var jc : AndroidJavaClass;
var jo : AndroidJavaObject;
var stringToEdit : String;

function Start () {
	last_X[id] = X_Offset;
	last_Y[id] = Y_Offset;
	last_Z[id] = Z_Offset;
	stringToEdit = "0.707,-0.707,0,0.707,-0.707,0,-0.707,-0.707,0,-0.707,-0.707,0,";
	translateMessage();
	refreshDirection();
}

function Update () {
	jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
	jo = jc.GetStatic.<AndroidJavaObject>("currentActivity");
	stringToEdit = jo.Call.<String>("message");
	translateMessage();
	refreshDirection();
	GUI.Label(new Rect(20, 70, 300, 20), stringToEdit);
}

function refreshDirection () {
	var now_X : float = vectorArray[id * D];
	var now_Y : float = vectorArray[id * D + 1];
	var now_Z : float = vectorArray[id * D + 2];
	if (Mathf.Abs(now_X - last_X[id]) < 1e-3 && Mathf.Abs(now_Y - last_Y[id]) < 1e-3 && Mathf.Abs(now_Z - last_Z[id]) < 1e-3) {
		return;
	};
	var axis_X : float = (now_X + last_X[id]) / 2;
	var axis_Y : float = (now_Y + last_Y[id]) / 2;
	var axis_Z : float = (now_Z + last_Z[id]) / 2;
	var length : float = Mathf.Sqrt(axis_X * axis_X + axis_Y * axis_Y + axis_Z * axis_Z);
	axis_X = axis_X / length;
	axis_Y = axis_Y / length;
	axis_Z = axis_Z / length;
	transform.Rotate(new Vector3(axis_X, axis_Y, axis_Z), 180, Space.World);
	transform.Rotate(new Vector3(now_X, now_Y, now_Z), 180, Space.World);
	last_X[id] = now_X;
	last_Y[id] = now_Y;
	last_Z[id] = now_Z;
}

function translateMessage () {
	var strArray : String[];
	var separator : char[] = [","[0]];
	strArray = stringToEdit.Split(separator);
	vectorArray[0] = float.Parse(strArray[0]);
	vectorArray[1] = float.Parse(strArray[1]);
	vectorArray[2] = float.Parse(strArray[2]);
	vectorArray[3] = float.Parse(strArray[3]);
	vectorArray[4] = float.Parse(strArray[4]);
	vectorArray[5] = float.Parse(strArray[5]);
	vectorArray[6] = float.Parse(strArray[6]);
	vectorArray[7] = float.Parse(strArray[7]);
	vectorArray[8] = float.Parse(strArray[8]);
	vectorArray[9] = float.Parse(strArray[9]);
	vectorArray[10] = float.Parse(strArray[10]);
	vectorArray[11] = float.Parse(strArray[11]);
}
