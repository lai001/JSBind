import { A, B, SharedPtrB } from "ks";

for (let index = 0; index < 2; index++) {
    let a = new A(50 + index, 150 + index);
    let b = new B();
    a.b = b;
    b.data = 1500 + index;
    console.log(a.b.data, b.data);
    console.log(a.b == b);
    console.log(a.v4, a.v6);
    a.printB();
    console.log(b.str);
}

console.log(1, g_b);
console.log(2, g_b.str);
console.log(3, g_b1);
console.log(4, g_b1.str);

let shared_ptr_b = new SharedPtrB();

console.log(shared_ptr_b);
console.log(5, shared_ptr_b.str);
console.log(6, g_sharedptr_b);
console.log(7, g_sharedptr_b.str);
console.log(8, g_sharedptr_b1);
console.log(9, g_sharedptr_b1.str);

try {
    let a = new A();
    a.b = shared_ptr_b;
} catch (error) {
    console.log(10, error);
}

try {
    let a = new A();
    a.b = shared_ptr_b._get();
} catch (error) {
    console.log(11, error);
}