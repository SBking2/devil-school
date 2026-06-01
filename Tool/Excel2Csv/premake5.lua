workspace "Excel2Csv"
    configurations { "Debug", "Release" }
    architecture "x64"
    startproject "Excel2Csv"

    global_output_dir = "./"

    project "Excel2Csv"
        kind "ConsoleApp"
        language "C++"
        cppdialect "C++17"
        runtime "Debug"
        staticruntime "on"

        targetdir ("./")
        files { "Excel2Csv/src/**.cpp", "Excel2Csv/src/**.h", "Excel2Csv/src/**.hpp" }

        includedirs {
            "xlnt/include"
        }

        libdirs{

        }

        links{
            "xlnt"
        }
