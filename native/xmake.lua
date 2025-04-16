add_rules("mode.debug", "mode.release")

set_project("sharpspades")

add_requires("enet6")

target("sharpspades", function ()
    set_kind("shared")
    add_files("*.c")
    add_packages("enet6")
end)
