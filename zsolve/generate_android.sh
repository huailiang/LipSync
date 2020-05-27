# MIT License

# Copyright (c) 2020 huailiang
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
# 
# The above copyright notice and this permission notice shall be included in all
# copies or substantial portions of the Software.
# 
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
# SOFTWARE.


export ANDROID_NDK=~/Documents/software/eclipse/android-ndk-r10e

if [[ ! -d ${ANDROID_NDK} ]]; then
	echo "ndk file not found"
	exit 1
fi

mkdir -p build_v7a && cd build_v7a
cmake -DANDROID_ABI=armeabi-v7a -DCMAKE_TOOLCHAIN_FILE=../cmake/android.toolchain.cmake -DANDROID_TOOLCHAIN_NAME=arm-linux-androideabi-4.9 -DANDROID_NATIVE_API_LEVEL=android-16 ..
cd ..
cmake --build build_v7a --config Release
mkdir -p Plugins/Android/libs/armeabi-v7a/
cp build_v7a/libZSolver.so Plugins/Android/libs/armeabi-v7a/libZSolver.so

mkdir -p build_x86  && cd build_x86
cmake -DANDROID_ABI=x86 -DCMAKE_TOOLCHAIN_FILE=../cmake/android.toolchain.cmake -DANDROID_TOOLCHAIN_NAME=x86-clang3.5 -DANDROID_NATIVE_API_LEVEL=android-9 ../
cd ..
cmake --build build_x86 --config Release
mkdir -p Plugins/Android/libs/x86/
cp build_x86/libZSolver.so Plugins/Android/libs/x86/libZSolver.so