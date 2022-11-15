use core::ffi::c_void;

enum AudioFormat {
    Wave,
    MP3,
    FLAC
}

struct PWPlayerInfo {
    volume: f32,
    file_name: *const char,
    playing: bool,
    format: AudioFormat,
    fade_in_milliseconds: i32,
    fade_out_milliseconds: i32,
    data: *mut c_void
}

struct WaveFile {
    chunk_id: char,
}

fn start_player(info: &mut PWPlayerInfo) {
    println!("Native Pipewire backend: starting");
    info.format = AudioFormat::Wave;
    
}