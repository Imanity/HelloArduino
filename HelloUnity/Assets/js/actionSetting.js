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
var X_Offset = 0;
var Y_Offset = 0;
var Z_Offset = 0;

//肢体方向数据
var vectors = new Array();

function Start () {

}

function Update () {
	//For demo
	var rotate : float = Time.deltaTime * 100;
	transform.Rotate(Vector3.up * rotate, Space.World);
}

function setDirection (vectors) {
	/* 单位向量的坐标值
	x, y 为地面,z 垂直地面向上
	*/
	/*var x = vectors[id].x;
	var y = vectors[id].y;
	var z = vectors[id].z;*/
	//换算旋转
	/*
	Tips:
	//相对于世界坐标中心向右旋转物体
	transform.Rotate(Vector3.right * rotate, Space.World);
	//相对于世界坐标中心向上旋转物体
	transform.Rotate(Vector3.up * rotate, Space.World);
	//相对于世界坐标中心向左旋转物体
	transform.Rotate(Vector3.left * rotate, Space.World);
	*/
}
